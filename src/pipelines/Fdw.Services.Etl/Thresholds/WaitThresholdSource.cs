using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
/// Reads the level of thresholds on waiting runs the host is configured with (FDW-3161, FDW-3975): the level the server, each pipeline and each schedule states. A pipeline or
/// schedule with no level row states nothing and inherits. Nothing else is assumed: a server that states no level, a level that names a mode that is not an option, or
/// a level whose pipeline or schedule does not exist fails the read and names what is wrong. The thresholds themselves are attached to the owners as threshold types and
/// are read by the threshold source.
/// </summary>
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

    /// <inheritdoc />
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

        var rows = await _rows.Levels(gateway.Value, cancellationToken).ConfigureAwait(false);
        if (rows.IsFailure || rows.Value is null)
        {
            return rows.ToNewResult<WaitThresholdConfiguration>();
        }

        var stated = rows.Value.ToDictionary(row => Key(row.OwnerKind, Owned(row.OwnerName)), row => row, StringComparer.Ordinal);
        return await Configuration(services.GetRequiredService<IPipelineConfigurationProvider>(), gateway.Value, stated, cancellationToken).ConfigureAwait(false);
    }

    // The three levels, and the proof that every owner a level names exists.
    private async Task<IGenericResult<WaitThresholdConfiguration>> Configuration(
        IPipelineConfigurationProvider pipelineProvider, IConfigurationGateway gateway, Dictionary<string, WaitThresholdLevelRecord> stated, CancellationToken cancellationToken)
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

    // The server's level has no owner name; the empty text names that, it does not stand in for a name that is missing.
    private static string Owned(string? ownerName) => ownerName is null ? string.Empty : ownerName;

    // The server states its level in a row of its own and must state one; it has no level above it, so Inherit is not a mode it can take.
    private IGenericResult<WaitThresholdLayer> ServerLevel(Dictionary<string, WaitThresholdLevelRecord> stated)
    {
        if (!stated.TryGetValue(Key(WaitThresholdOwners.Server, string.Empty), out var row))
        {
            return GenericResult<WaitThresholdLayer>.Failure(WaitThresholdLog.ServerLevelMissing(_logger, null));
        }

        var mode = WaitThresholdModes.ByName(row.Mode);
        return ReferenceEquals(mode, WaitThresholdModes.NotFound) || mode.InheritsFromParent
            ? GenericResult<WaitThresholdLayer>.Failure(WaitThresholdLog.ServerLevelMissing(_logger, row.Mode))
            : Level(WaitThresholdOwners.Server, row.Mode);
    }

    private IGenericResult<WaitThresholdLayer> Level(string owner, string modeName) =>
        ReferenceEquals(WaitThresholdModes.ByName(modeName), WaitThresholdModes.NotFound)
            ? GenericResult<WaitThresholdLayer>.Failure(WaitThresholdLog.ModeUnknown(_logger, owner, modeName))
            : GenericResult<WaitThresholdLayer>.Success(new WaitThresholdLayer(owner, WaitThresholdModes.ByName(modeName)));

    // Every pipeline has a level: the one its row states, or Inherit when it has no row.
    private async Task<IGenericResult<Dictionary<string, WaitThresholdLayer>>> PipelineLevels(
        IPipelineConfigurationProvider pipelines, Dictionary<string, WaitThresholdLevelRecord> stated, CancellationToken cancellationToken)
    {
        var all = await pipelines.Get(cancellationToken).ConfigureAwait(false);
        return all.IsFailure || all.Value is null
            ? all.ToNewResult<Dictionary<string, WaitThresholdLayer>>()
            : Levels(WaitThresholdOwners.Pipeline, all.Value.Select(pipeline => pipeline.Name), stated);
    }

    private async Task<IGenericResult<Dictionary<string, WaitThresholdLayer>>> ScheduleLevels(
        IConfigurationGateway gateway, Dictionary<string, WaitThresholdLevelRecord> stated, CancellationToken cancellationToken)
    {
        var names = await _rows.Active<ScheduleNameRecord>(gateway, WaitThresholdRows.SchedulePath, WaitThresholdRows.ScheduleContainer, cancellationToken).ConfigureAwait(false);
        return names.IsFailure || names.Value is null
            ? names.ToNewResult<Dictionary<string, WaitThresholdLayer>>()
            : Levels(WaitThresholdOwners.Schedule, names.Value.Select(row => row.Name), stated);
    }

    private IGenericResult<Dictionary<string, WaitThresholdLayer>> Levels(string kind, IEnumerable<string> names, Dictionary<string, WaitThresholdLevelRecord> stated)
    {
        var levels = new Dictionary<string, WaitThresholdLayer>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in names)
        {
            var mode = stated.TryGetValue(Key(kind, name), out var row) ? row.Mode : InheritMode;
            var level = Level(string.Concat(kind, ":", name), mode);
            if (level.IsFailure || level.Value is null)
            {
                return level.ToNewResult<Dictionary<string, WaitThresholdLayer>>();
            }

            levels[name] = level.Value;
        }

        return GenericResult<Dictionary<string, WaitThresholdLayer>>.Success(levels);
    }

    // A level whose owner is not a pipeline or a schedule that exists would never apply; it fails the read instead of lying unused.
    private IGenericResult OwnersExist(Dictionary<string, WaitThresholdLevelRecord> stated, Dictionary<string, WaitThresholdLayer> pipelines, Dictionary<string, WaitThresholdLayer> schedules)
    {
        foreach (var row in stated.Values)
        {
            var owner = Owned(row.OwnerName);
            var exists = row.OwnerKind switch
            {
                WaitThresholdOwners.Pipeline => pipelines.ContainsKey(owner),
                WaitThresholdOwners.Schedule => schedules.ContainsKey(owner),
                _ => true,
            };
            if (!exists)
            {
                return GenericResult.Failure(WaitThresholdLog.OwnerUnknown(_logger, "level", row.OwnerKind, owner));
            }
        }

        return GenericResult.Success();
    }
}
