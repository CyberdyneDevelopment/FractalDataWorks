using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Etl.Abstractions.Monitoring.AlertSeverityOptions;
using Fdw.Etl.Abstractions.Monitoring.ComparisonOperatorOptions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Abstractions.Thresholds;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Thresholds;

/// <summary>
/// Reads and writes the level of thresholds on waiting runs an owner states (FDW-3161). A write judges the level (see <see cref="WaitThresholdValidator"/>), then what only the
/// data can say: the owner is a pipeline or schedule that exists, every notification rule it names exists, and every pipeline it counts exists. It then replaces the owner's
/// thresholds and level together: the old rows are retired and the new ones saved, so a level is never Own with none unless a write fails between the two, which is reported.
/// </summary>
public sealed class WaitThresholdStore : IWaitThresholdStore
{
    private readonly IConfigurationGatewayProvider _gateways;
    private readonly IPipelineConfigurationProvider _pipelines;
    private readonly IRuleNotifier _notifier;
    private readonly ILogger _logger;
    private readonly WaitThresholdRows _rows;

    /// <summary>Initializes a new instance of the <see cref="WaitThresholdStore"/> class.</summary>
    /// <param name="gateways">Reaches the configuration database.</param>
    /// <param name="pipelines">Finds the pipelines.</param>
    /// <param name="notifier">Says whether a notification rule exists.</param>
    /// <param name="logger">The logger.</param>
    public WaitThresholdStore(
        IConfigurationGatewayProvider gateways, IPipelineConfigurationProvider pipelines, IRuleNotifier notifier, ILogger<WaitThresholdStore>? logger = null)
    {
        _gateways = gateways ?? throw new ArgumentNullException(nameof(gateways));
        _pipelines = pipelines ?? throw new ArgumentNullException(nameof(pipelines));
        _notifier = notifier ?? throw new ArgumentNullException(nameof(notifier));
        _logger = logger ?? NullLogger<WaitThresholdStore>.Instance;
        _rows = new WaitThresholdRows(_logger);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<WaitThresholdLevelPayload>> Read(string ownerKind, string? ownerName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerKind);
        var gateway = _gateways.Get(EtlPipelineTypes.ConfigurationConnection);
        if (gateway.IsFailure || gateway.Value is null)
        {
            return gateway.ToNewResult<WaitThresholdLevelPayload>();
        }

        var exists = await OwnerExists(gateway.Value, ownerKind, ownerName, cancellationToken).ConfigureAwait(false);
        if (exists.IsFailure)
        {
            return exists.ToNewResult<WaitThresholdLevelPayload>();
        }

        var tables = await _rows.Tables(gateway.Value, cancellationToken).ConfigureAwait(false);
        if (tables.IsFailure || tables.Value is null)
        {
            return tables.ToNewResult<WaitThresholdLevelPayload>();
        }

        var level = tables.Value.Levels.FirstOrDefault(row => WaitThresholdRows.Owns(ownerKind, ownerName, row.OwnerKind, row.OwnerName));
        return GenericResult<WaitThresholdLevelPayload>.Success(new WaitThresholdLevelPayload
        {
            Mode = level is null ? InheritMode : level.Mode,
            Thresholds = [.. tables.Value.Thresholds.Where(row => WaitThresholdRows.Owns(ownerKind, ownerName, row.OwnerKind, row.OwnerName)).Select(row => Payload(row, tables.Value.Rules))],
        });
    }

    // A level that states nothing is the mode Inherit; an owner that has no row is read as it.
    private const string InheritMode = "Inherit";

    private static WaitThresholdPayload Payload(WaitThresholdRecord row, IReadOnlyList<WaitThresholdRuleRecord> rules) => new()
    {
        Name = row.Name,
        CountedPipeline = row.CountedPipeline,
        Comparison = row.Comparison,
        Number = row.Number,
        Severity = row.Severity,
        NotificationRules = [.. rules.Where(rule => rule.WaitThresholdId == row.Id).OrderBy(rule => rule.Ordinal).Select(rule => rule.RuleName)],
        StopsNewRuns = row.StopsNewRuns,
        IsEnabled = row.IsEnabled,
    };

    /// <inheritdoc/>
    public async Task<IGenericResult<WaitThresholdLevelPayload>> Write(
        string ownerKind, string? ownerName, WaitThresholdLevelPayload level, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerKind);
        ArgumentNullException.ThrowIfNull(level);
        var judged = WaitThresholdValidator.Check(ownerKind, level, _logger);
        if (judged.IsFailure)
        {
            return judged.ToNewResult<WaitThresholdLevelPayload>();
        }

        var gateway = _gateways.Get(EtlPipelineTypes.ConfigurationConnection);
        if (gateway.IsFailure || gateway.Value is null)
        {
            return gateway.ToNewResult<WaitThresholdLevelPayload>();
        }

        var data = await DataExists(gateway.Value, ownerKind, ownerName, level, cancellationToken).ConfigureAwait(false);
        if (data.IsFailure)
        {
            return data.ToNewResult<WaitThresholdLevelPayload>();
        }

        var written = await Replace(gateway.Value, ownerKind, ownerName, level, cancellationToken).ConfigureAwait(false);
        return written.IsFailure
            ? written.ToNewResult<WaitThresholdLevelPayload>()
            : await Read(ownerKind, ownerName, cancellationToken).ConfigureAwait(false);
    }

    // The owner exists, and so do the notification rules and the pipelines the thresholds name.
    private async Task<IGenericResult> DataExists(
        IConfigurationGateway gateway, string ownerKind, string? ownerName, WaitThresholdLevelPayload level, CancellationToken cancellationToken)
    {
        var owner = await OwnerExists(gateway, ownerKind, ownerName, cancellationToken).ConfigureAwait(false);
        if (owner.IsFailure)
        {
            return owner;
        }

        foreach (var rule in level.Thresholds.SelectMany(threshold => threshold.NotificationRules).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var found = await _notifier.Exists(rule, cancellationToken).ConfigureAwait(false);
            if (found.IsFailure)
            {
                return GenericResult.Failure(WaitThresholdLog.Refused(_logger, "notificationRules", $"the notification rule '{rule}' does not exist"));
            }
        }

        var counted = level.Thresholds.Where(threshold => threshold.CountedPipeline is not null).Select(threshold => threshold.CountedPipeline!).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return counted.Count == 0 ? GenericResult.Success() : await PipelinesExist(counted, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IGenericResult> PipelinesExist(List<string> counted, CancellationToken cancellationToken)
    {
        var all = await _pipelines.Get(cancellationToken).ConfigureAwait(false);
        if (all.IsFailure || all.Value is null)
        {
            return all;
        }

        var missing = counted.FirstOrDefault(name => !all.Value.Any(pipeline => string.Equals(pipeline.Name, name, StringComparison.OrdinalIgnoreCase)));
        return missing is null
            ? GenericResult.Success()
            : GenericResult.Failure(WaitThresholdLog.Refused(_logger, "countedPipeline", $"there is no pipeline '{missing}' to count"));
    }

    private async Task<IGenericResult> OwnerExists(IConfigurationGateway gateway, string ownerKind, string? ownerName, CancellationToken cancellationToken)
    {
        if (string.Equals(ownerKind, WaitThresholdOwners.Server, StringComparison.Ordinal))
        {
            return GenericResult.Success();
        }

        if (string.IsNullOrWhiteSpace(ownerName))
        {
            return GenericResult.Failure(WaitThresholdLog.Refused(_logger, "owner", $"a {ownerKind} is named"));
        }

        var names = await OwnerNames(gateway, ownerKind, cancellationToken).ConfigureAwait(false);
        if (names.IsFailure || names.Value is null)
        {
            return names;
        }

        return names.Value.Any(name => string.Equals(name, ownerName, StringComparison.OrdinalIgnoreCase))
            ? GenericResult.Success()
            : GenericResult.Failure(WaitThresholdLog.NoSuchOwner(_logger, ownerKind, ownerName));
    }

    private async Task<IGenericResult<IReadOnlyList<string>>> OwnerNames(IConfigurationGateway gateway, string ownerKind, CancellationToken cancellationToken)
    {
        if (string.Equals(ownerKind, WaitThresholdOwners.Pipeline, StringComparison.Ordinal))
        {
            var pipelines = await _pipelines.Get(cancellationToken).ConfigureAwait(false);
            return pipelines.IsFailure || pipelines.Value is null
                ? pipelines.ToNewResult<IReadOnlyList<string>>()
                : GenericResult<IReadOnlyList<string>>.Success([.. pipelines.Value.Select(pipeline => pipeline.Name)]);
        }

        var schedules = await _rows.Active<ScheduleNameRecord>(gateway, WaitThresholdRows.SchedulePath, WaitThresholdRows.ScheduleContainer, cancellationToken).ConfigureAwait(false);
        return schedules.IsFailure || schedules.Value is null
            ? schedules.ToNewResult<IReadOnlyList<string>>()
            : GenericResult<IReadOnlyList<string>>.Success([.. schedules.Value.Select(row => row.Name)]);
    }

    // The owner's old thresholds, their rules and the old level are retired, then the new level, thresholds and rules are saved.
    private async Task<IGenericResult> Replace(IConfigurationGateway gateway, string ownerKind, string? ownerName, WaitThresholdLevelPayload level, CancellationToken cancellationToken)
    {
        var tables = await _rows.Tables(gateway, cancellationToken).ConfigureAwait(false);
        if (tables.IsFailure || tables.Value is null)
        {
            return tables;
        }

        var oldThresholds = tables.Value.Thresholds.Where(row => WaitThresholdRows.Owns(ownerKind, ownerName, row.OwnerKind, row.OwnerName)).ToList();
        var oldRules = tables.Value.Rules.Where(rule => oldThresholds.Any(row => row.Id == rule.WaitThresholdId)).Select(rule => rule.Id).ToList();
        var oldLevel = tables.Value.Levels.FirstOrDefault(row => WaitThresholdRows.Owns(ownerKind, ownerName, row.OwnerKind, row.OwnerName));

        var retired = await WaitThresholdRows.Retire(gateway, WaitThresholdRows.RuleContainer, oldRules, cancellationToken).ConfigureAwait(false);
        retired = retired.IsFailure ? retired : await WaitThresholdRows.Retire(gateway, WaitThresholdRows.ThresholdContainer, [.. oldThresholds.Select(row => row.Id)], cancellationToken).ConfigureAwait(false);
        if (retired.IsFailure)
        {
            return Failed(ownerKind, ownerName, retired);
        }

        var saved = await SaveLevel(gateway, ownerKind, ownerName, level, oldLevel, cancellationToken).ConfigureAwait(false);
        saved = saved.IsFailure ? saved : await SaveThresholds(gateway, ownerKind, ownerName, level, cancellationToken).ConfigureAwait(false);
        return saved.IsFailure ? Failed(ownerKind, ownerName, saved) : saved;
    }

    private IGenericResult Failed(string ownerKind, string? ownerName, IGenericResult failure) =>
        GenericResult.Failure(WaitThresholdLog.NotWritten(_logger, ownerName is null ? ownerKind : string.Concat(ownerKind, ":", ownerName), failure.CurrentMessage));

    // A level that states nothing has no row: Inherit retires the owner's.
    private static async Task<IGenericResult> SaveLevel(
        IConfigurationGateway gateway, string ownerKind, string? ownerName, WaitThresholdLevelPayload level, WaitThresholdLevelRecord? old, CancellationToken cancellationToken)
    {
        var mode = WaitThresholdModes.ByName(level.Mode!);
        if (mode.InheritsFromParent)
        {
            return old is null ? GenericResult.Success() : await WaitThresholdRows.Retire(gateway, WaitThresholdRows.LevelContainer, [old.Id], cancellationToken).ConfigureAwait(false);
        }

        return await Save(
            gateway, WaitThresholdRows.LevelContainer,
            new WaitThresholdLevelRecord { Id = old is null ? Guid.CreateVersion7() : old.Id, OwnerKind = ownerKind, OwnerName = ownerName, Mode = mode.Name },
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IGenericResult> SaveThresholds(
        IConfigurationGateway gateway, string ownerKind, string? ownerName, WaitThresholdLevelPayload level, CancellationToken cancellationToken)
    {
        foreach (var threshold in level.Thresholds)
        {
            var id = Guid.CreateVersion7();
            var saved = await Save(
                gateway, WaitThresholdRows.ThresholdContainer,
                new WaitThresholdRecord
                {
                    Id = id, OwnerKind = ownerKind, OwnerName = ownerName, Name = threshold.Name!, CountedPipeline = threshold.CountedPipeline,
                    Comparison = ComparisonName(threshold.Comparison!), Number = threshold.Number!.Value, Severity = SeverityName(threshold.Severity!),
                    StopsNewRuns = threshold.StopsNewRuns!.Value, IsEnabled = threshold.IsEnabled!.Value,
                },
                cancellationToken).ConfigureAwait(false);
            for (var ordinal = 0; ordinal < threshold.NotificationRules.Count && saved.IsSuccess; ordinal++)
            {
                saved = await Save(
                    gateway, WaitThresholdRows.RuleContainer,
                    new WaitThresholdRuleRecord { Id = Guid.CreateVersion7(), WaitThresholdId = id, RuleName = threshold.NotificationRules[ordinal], Ordinal = ordinal },
                    cancellationToken).ConfigureAwait(false);
            }

            if (saved.IsFailure)
            {
                return saved;
            }
        }

        return GenericResult.Success();
    }

    // The names as the options spell them, so a row never holds a different spelling of the same option.
    private static string ComparisonName(string name) => ComparisonOperators.ByName(name).Name;

    private static string SeverityName(string name) => AlertSeverities.ByName(name).Name;

    private static async Task<IGenericResult> Save<T>(IConfigurationGateway gateway, string container, T row, CancellationToken cancellationToken)
        where T : class
        => await gateway.Execute<ConfigurationWriteResult>(new ConfigurationSaveCommand<T>(row, replacesCurrent: true), WaitThresholdRows.Target(container), cancellationToken).ConfigureAwait(false);
}
