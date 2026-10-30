using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Etl.Abstractions.Monitoring.AlertSeverityOptions;
using Fdw.Etl.Abstractions.Monitoring.ComparisonOperatorOptions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Abstractions.Thresholds;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Thresholds;

/// <summary>
/// Reads the thresholds on waiting runs the host is configured with (FDW-3161): the level the server, each pipeline and each schedule states, and the thresholds
/// each owns with the notification rule names of each. A pipeline or schedule with no level row states nothing and inherits. Nothing else is assumed: a server
/// that states no level, a mode or an option that does not exist, an owner that does not exist, or rows that cannot be read fail the read, naming it.
/// </summary>
/// <remarks>Read under the system context, since no request is in flight when the monitor judges the waiting runs, and the rows belong to every tenant.</remarks>
public sealed class WaitThresholdSource : IWaitThresholdSource
{
    // A level that states nothing takes the level above it: the mode named Inherit. That is what having no row means, not a stand-in for a value that is missing.
    private const string InheritMode = "Inherit";

    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger _logger;
    private readonly WaitThresholdRows _rows;

    /// <summary>Initializes a new instance of the <see cref="WaitThresholdSource"/> class.</summary>
    /// <param name="scopes">Opens the scope the configuration is read in.</param>
    /// <param name="logger">The logger.</param>
    public WaitThresholdSource(IServiceScopeFactory scopes, ILogger<WaitThresholdSource>? logger = null)
    {
        _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        _logger = logger ?? NullLogger<WaitThresholdSource>.Instance;
        _rows = new WaitThresholdRows(_logger);
    }

    private sealed record Stated(
        Dictionary<string, List<WaitThreshold>> ByOwner, IReadOnlyList<(string Kind, string Owner, string Threshold)> Owners, Dictionary<string, WaitThresholdLevelRecord> Levels);

    /// <inheritdoc/>
    public async Task<IGenericResult<WaitThresholdConfiguration>> Read(CancellationToken cancellationToken = default)
    {
        var scope = _scopes.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            using (new SystemAuthenticationContextScope(scope.ServiceProvider.GetRequiredService<IAuthenticationContextAccessor>()))
            {
                return await ReadAll(scope.ServiceProvider, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task<IGenericResult<WaitThresholdConfiguration>> ReadAll(IServiceProvider services, CancellationToken cancellationToken)
    {
        var gateway = services.GetRequiredService<IConfigurationGatewayProvider>().Get(EtlPipelineTypes.ConfigurationConnection);
        if (gateway.IsFailure || gateway.Value is null)
        {
            return gateway.ToNewResult<WaitThresholdConfiguration>();
        }

        var stated = await Thresholds(gateway.Value, cancellationToken).ConfigureAwait(false);
        if (stated.IsFailure || stated.Value is null)
        {
            return stated.ToNewResult<WaitThresholdConfiguration>();
        }

        return await Configuration(services.GetRequiredService<IPipelineConfigurationProvider>(), gateway.Value, stated.Value, cancellationToken).ConfigureAwait(false);
    }

    // The three levels, and the proof that every owner a level or threshold names exists.
    private async Task<IGenericResult<WaitThresholdConfiguration>> Configuration(
        IPipelineConfigurationProvider pipelineProvider, IConfigurationGateway gateway, Stated stated, CancellationToken cancellationToken)
    {
        var pipelines = await PipelineLevels(pipelineProvider, stated, cancellationToken).ConfigureAwait(false);
        if (pipelines.IsFailure || pipelines.Value is null)
        {
            return pipelines.ToNewResult<WaitThresholdConfiguration>();
        }

        var schedules = await ScheduleLevels(gateway, stated, cancellationToken).ConfigureAwait(false);
        if (schedules.IsFailure || schedules.Value is null)
        {
            return schedules.ToNewResult<WaitThresholdConfiguration>();
        }

        var server = ServerLevel(stated);
        if (server.IsFailure || server.Value is null)
        {
            return server.ToNewResult<WaitThresholdConfiguration>();
        }

        var owned = OwnersExist(stated, pipelines.Value, schedules.Value);
        return owned.IsFailure
            ? owned.ToNewResult<WaitThresholdConfiguration>()
            : GenericResult<WaitThresholdConfiguration>.Success(new WaitThresholdConfiguration(server.Value, pipelines.Value, schedules.Value));
    }

    private static string Key(string kind, string name) => string.Concat(kind, "/", name).ToUpperInvariant();

    // The server's level and thresholds have no owner name; the empty text names that, it does not stand in for a name that is missing.
    private static string Owned(string? ownerName) => ownerName is null ? string.Empty : ownerName;

    // The server states its level in a row of its own and must state one; it has no level above it, so Inherit is not a mode it can take.
    private IGenericResult<WaitThresholdLayer> ServerLevel(Stated stated)
    {
        if (!stated.Levels.TryGetValue(Key(WaitThresholdOwners.Server, string.Empty), out var row))
        {
            return GenericResult<WaitThresholdLayer>.Failure(WaitThresholdLog.ServerLevelMissing(_logger, null));
        }

        var mode = WaitThresholdModes.ByName(row.Mode);
        return ReferenceEquals(mode, WaitThresholdModes.NotFound) || mode.InheritsFromParent
            ? GenericResult<WaitThresholdLayer>.Failure(WaitThresholdLog.ServerLevelMissing(_logger, row.Mode))
            : Level(WaitThresholdOwners.Server, row.Mode, stated.ByOwner.GetValueOrDefault(Key(WaitThresholdOwners.Server, string.Empty), []));
    }

    private IGenericResult<WaitThresholdLayer> Level(string owner, string modeName, List<WaitThreshold> thresholds) =>
        ReferenceEquals(WaitThresholdModes.ByName(modeName), WaitThresholdModes.NotFound)
            ? GenericResult<WaitThresholdLayer>.Failure(WaitThresholdLog.ModeUnknown(_logger, owner, modeName))
            : GenericResult<WaitThresholdLayer>.Success(new WaitThresholdLayer(owner, WaitThresholdModes.ByName(modeName), thresholds));

    // Every pipeline has a level: the one its row states, or Inherit when it has no row.
    private async Task<IGenericResult<Dictionary<string, WaitThresholdLayer>>> PipelineLevels(
        IPipelineConfigurationProvider pipelines, Stated stated, CancellationToken cancellationToken)
    {
        var all = await pipelines.Get(cancellationToken).ConfigureAwait(false);
        return all.IsFailure || all.Value is null
            ? all.ToNewResult<Dictionary<string, WaitThresholdLayer>>()
            : Levels(WaitThresholdOwners.Pipeline, all.Value.Select(pipeline => pipeline.Name), stated);
    }

    private async Task<IGenericResult<Dictionary<string, WaitThresholdLayer>>> ScheduleLevels(
        IConfigurationGateway gateway, Stated stated, CancellationToken cancellationToken)
    {
        var names = await _rows.Active<ScheduleNameRecord>(gateway, WaitThresholdRows.SchedulePath, WaitThresholdRows.ScheduleContainer, cancellationToken).ConfigureAwait(false);
        return names.IsFailure || names.Value is null
            ? names.ToNewResult<Dictionary<string, WaitThresholdLayer>>()
            : Levels(WaitThresholdOwners.Schedule, names.Value.Select(row => row.Name), stated);
    }

    private IGenericResult<Dictionary<string, WaitThresholdLayer>> Levels(string kind, IEnumerable<string> names, Stated stated)
    {
        var levels = new Dictionary<string, WaitThresholdLayer>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in names)
        {
            var mode = stated.Levels.TryGetValue(Key(kind, name), out var row) ? row.Mode : InheritMode;
            var level = Level(string.Concat(kind, ":", name), mode, stated.ByOwner.GetValueOrDefault(Key(kind, name), []));
            if (level.IsFailure || level.Value is null)
            {
                return level.ToNewResult<Dictionary<string, WaitThresholdLayer>>();
            }

            levels[name] = level.Value;
        }

        return GenericResult<Dictionary<string, WaitThresholdLayer>>.Success(levels);
    }

    // A level or a threshold whose owner is not a pipeline or a schedule that exists would never apply; it fails the read instead of lying unused.
    private IGenericResult OwnersExist(Stated stated, Dictionary<string, WaitThresholdLayer> pipelines, Dictionary<string, WaitThresholdLayer> schedules)
    {
        var owners = stated.Owners.Concat(stated.Levels.Values.Select(row => (row.OwnerKind, Owned(row.OwnerName), "level")));
        foreach (var (kind, owner, label) in owners)
        {
            var exists = kind switch
            {
                WaitThresholdOwners.Pipeline => pipelines.ContainsKey(owner),
                WaitThresholdOwners.Schedule => schedules.ContainsKey(owner),
                _ => true,
            };
            if (!exists)
            {
                return GenericResult.Failure(WaitThresholdLog.OwnerUnknown(_logger, label, kind, owner));
            }
        }

        return GenericResult.Success();
    }

    // The level rows and the threshold rows, each threshold with the notification rule names it sends, grouped by who states them.
    private async Task<IGenericResult<Stated>> Thresholds(IConfigurationGateway gateway, CancellationToken cancellationToken)
    {
        var tables = await _rows.Tables(gateway, cancellationToken).ConfigureAwait(false);
        if (tables.IsFailure || tables.Value is null)
        {
            return tables.ToNewResult<Stated>();
        }

        var ruleNames = tables.Value.Rules.GroupBy(rule => rule.WaitThresholdId).ToDictionary(group => group.Key, group => (IReadOnlyList<string>)[.. group.OrderBy(rule => rule.Ordinal).Select(rule => rule.RuleName)]);
        var byOwner = new Dictionary<string, List<WaitThreshold>>(StringComparer.Ordinal);
        var owners = new List<(string Kind, string Owner, string Threshold)>();
        foreach (var row in tables.Value.Thresholds)
        {
            var built = Build(row, ruleNames.GetValueOrDefault(row.Id, []));
            if (built.IsFailure || built.Value is null)
            {
                return built.ToNewResult<Stated>();
            }

            var key = Key(row.OwnerKind, Owned(row.OwnerName));
            if (!byOwner.TryGetValue(key, out var list))
            {
                byOwner[key] = list = [];
            }

            list.Add(built.Value);
            owners.Add((row.OwnerKind, Owned(row.OwnerName), string.Concat("threshold '", row.Name, "'")));
        }

        return GenericResult<Stated>.Success(
            new Stated(byOwner, owners, tables.Value.Levels.ToDictionary(row => Key(row.OwnerKind, Owned(row.OwnerName)), row => row, StringComparer.Ordinal)));
    }

    private IGenericResult<WaitThreshold> Build(WaitThresholdRecord row, IReadOnlyList<string> rules)
    {
        var owner = row.OwnerName is null ? row.OwnerKind : string.Concat(row.OwnerKind, ":", row.OwnerName);
        if (row.OwnerKind is not (WaitThresholdOwners.Server or WaitThresholdOwners.Pipeline or WaitThresholdOwners.Schedule))
        {
            return GenericResult<WaitThreshold>.Failure(WaitThresholdLog.OptionUnknown(_logger, row.Name, owner, "owner kind", row.OwnerKind));
        }

        if (ReferenceEquals(ComparisonOperators.ByName(row.Comparison), ComparisonOperators.NotFound))
        {
            return GenericResult<WaitThreshold>.Failure(WaitThresholdLog.OptionUnknown(_logger, row.Name, owner, "comparison", row.Comparison));
        }

        if (ReferenceEquals(AlertSeverities.ByName(row.Severity), AlertSeverities.NotFound))
        {
            return GenericResult<WaitThreshold>.Failure(WaitThresholdLog.OptionUnknown(_logger, row.Name, owner, "severity", row.Severity));
        }

        return GenericResult<WaitThreshold>.Success(new WaitThreshold
        {
            Owner = owner,
            Name = row.Name,
            CountedPipeline = row.CountedPipeline,
            Comparison = ComparisonOperators.ByName(row.Comparison),
            Number = row.Number,
            Severity = AlertSeverities.ByName(row.Severity),
            NotificationRules = rules,
            StopsNewRuns = row.StopsNewRuns,
            IsEnabled = row.IsEnabled,
        });
    }
}
