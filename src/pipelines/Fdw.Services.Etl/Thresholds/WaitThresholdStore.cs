using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Abstractions.Thresholds;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Thresholds;

/// <summary>
/// Reads and writes the level of thresholds on waiting runs an owner states (FDW-3161, FDW-3975). A write judges the level (see <see cref="WaitThresholdValidator"/>), then what
/// only the data can say: the owner is a pipeline or schedule that exists. It then replaces the owner's level: Inherit retires the row, any other mode saves it.
/// </summary>
public sealed class WaitThresholdStore : IWaitThresholdStore
{
    // A level that states nothing is the mode Inherit; an owner that has no row is read as it.
    private const string InheritMode = "Inherit";

    private readonly IConfigurationGatewayProvider _gateways;
    private readonly IPipelineConfigurationProvider _pipelines;
    private readonly ILogger _logger;
    private readonly WaitThresholdRows _rows;

    /// <summary>Initializes a new instance of the <see cref="WaitThresholdStore"/> class.</summary>
    /// <param name="gateways">Reaches the configuration database.</param>
    /// <param name="pipelines">Finds the pipelines.</param>
    /// <param name="logger">The logger.</param>
    public WaitThresholdStore(IConfigurationGatewayProvider gateways, IPipelineConfigurationProvider pipelines, ILogger<WaitThresholdStore>? logger = null)
    {
        _gateways = gateways;
        _pipelines = pipelines;
        _logger = logger ?? NullLogger<WaitThresholdStore>.Instance;
        _rows = new WaitThresholdRows(_logger);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<WaitThresholdLevelPayload>> Read(string ownerKind, string? ownerName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ownerKind))
            return GenericResult<WaitThresholdLevelPayload>.Failure(WaitThresholdLog.OwnerKindRequired(_logger));

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

        var levels = await _rows.Levels(gateway.Value, cancellationToken).ConfigureAwait(false);
        if (levels.IsFailure || levels.Value is null)
        {
            return levels.ToNewResult<WaitThresholdLevelPayload>();
        }

        var level = levels.Value.FirstOrDefault(row => WaitThresholdRows.Owns(ownerKind, ownerName, row.OwnerKind, row.OwnerName));
        return GenericResult<WaitThresholdLevelPayload>.Success(new WaitThresholdLevelPayload { Mode = level is null ? InheritMode : level.Mode });
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<WaitThresholdLevelPayload>> Write(
        string ownerKind, string? ownerName, WaitThresholdLevelPayload level, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ownerKind))
            return GenericResult<WaitThresholdLevelPayload>.Failure(WaitThresholdLog.OwnerKindRequired(_logger));

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

        var owner = await OwnerExists(gateway.Value, ownerKind, ownerName, cancellationToken).ConfigureAwait(false);
        if (owner.IsFailure)
        {
            return owner.ToNewResult<WaitThresholdLevelPayload>();
        }

        var written = await Replace(gateway.Value, ownerKind, ownerName, level, cancellationToken).ConfigureAwait(false);
        return written.IsFailure
            ? written.ToNewResult<WaitThresholdLevelPayload>()
            : await Read(ownerKind, ownerName, cancellationToken).ConfigureAwait(false);
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

    // The owner's old level is replaced: Inherit retires it, any other mode saves the new one over it.
    private async Task<IGenericResult> Replace(IConfigurationGateway gateway, string ownerKind, string? ownerName, WaitThresholdLevelPayload level, CancellationToken cancellationToken)
    {
        var levels = await _rows.Levels(gateway, cancellationToken).ConfigureAwait(false);
        if (levels.IsFailure || levels.Value is null)
        {
            return levels;
        }

        var old = levels.Value.FirstOrDefault(row => WaitThresholdRows.Owns(ownerKind, ownerName, row.OwnerKind, row.OwnerName));
        var saved = await SaveLevel(gateway, ownerKind, ownerName, level, old, cancellationToken).ConfigureAwait(false);
        return saved.IsFailure
            ? GenericResult.Failure(WaitThresholdLog.NotWritten(_logger, ownerName is null ? ownerKind : string.Concat(ownerKind, ":", ownerName), saved.CurrentMessage))
            : saved;
    }

    // A level that states nothing has no row: Inherit retires the owner's.
    private static async Task<IGenericResult> SaveLevel(
        IConfigurationGateway gateway, string ownerKind, string? ownerName, WaitThresholdLevelPayload level, WaitThresholdLevelRecord? old, CancellationToken cancellationToken)
    {
        var mode = WaitThresholdModes.ByName(level.Mode!);
        if (mode.InheritsFromParent)
        {
            return old is null ? GenericResult.Success() : await WaitThresholdRows.Retire(gateway, WaitThresholdRows.LevelContainer, [old.Id], cancellationToken).ConfigureAwait(false);
        }

        var row = new WaitThresholdLevelRecord { Id = old is null ? Guid.CreateVersion7() : old.Id, OwnerKind = ownerKind, OwnerName = ownerName, Mode = mode.Name };
        return await gateway.Execute<ConfigurationWriteResult>(
            new ConfigurationSaveCommand<WaitThresholdLevelRecord>(row, replacesCurrent: true), WaitThresholdRows.Target(WaitThresholdRows.LevelContainer), cancellationToken).ConfigureAwait(false);
    }
}
