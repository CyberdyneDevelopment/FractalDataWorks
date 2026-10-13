using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Results;
using Fdw.Services.Pipelines.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Decides whether a run may act with the roles it was started with, for the pipeline it was started on (FDW-1087, FDW-1161).
/// </summary>
/// <remarks>
/// <para>
/// A run acts as its tenant run: the pipeline's tenant plus the roles whoever started it named. The roles belong to the run, not
/// to the pipeline, so a manual trigger names them in its body and a schedule records the ones its creator chose; whoever names
/// them may name only roles they hold themselves in the tenant they act in, so starting a run never grants more than its starter
/// holds.
/// </para>
/// <para>
/// Nothing at run time re-checks a permission per resource: the run's roles and the tenant's row-level security are all that
/// stand between a pipeline and the data it touches. So the roles are judged here, once, for everything the pipeline reaches.
/// Every data set it reads or writes must be visible to the caller and the roles must carry the matching datasets permission,
/// and the connections and data stores the data set's sources sit on must be visible and carry the matching connections and
/// datastores permissions; a connection the pipeline names itself, and a data store container it reads or writes, are judged the
/// same way.
/// </para>
/// </remarks>
public sealed class PipelineRunAuthority
{
    private const string DataSetRead = "datasets:read";
    private const string DataSetWrite = "datasets:write";
    private const string ConnectionRead = "connections:read";
    private const string ConnectionWrite = "connections:write";
    private const string DataStoreRead = "datastores:read";
    private const string DataStoreWrite = "datastores:write";

    private readonly IRoleGrants _grants;
    private readonly IDataSetConfigurationProvider _dataSets;
    private readonly IConnectionConfigurationProvider _connections;
    private readonly IDataStoreProvider _dataStores;

    /// <summary>Initializes a new instance of the <see cref="PipelineRunAuthority"/> class.</summary>
    /// <param name="grants">Expands the roles a run acts with into their permissions.</param>
    /// <param name="dataSets">Resolves data sets as the caller.</param>
    /// <param name="connections">Resolves connections as the caller.</param>
    /// <param name="dataStores">Resolves data stores and their containers as the caller.</param>
    public PipelineRunAuthority(
        IRoleGrants grants,
        IDataSetConfigurationProvider dataSets,
        IConnectionConfigurationProvider connections,
        IDataStoreProvider dataStores)
    {
        _grants = grants ?? throw new ArgumentNullException(nameof(grants));
        _dataSets = dataSets ?? throw new ArgumentNullException(nameof(dataSets));
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));
        _dataStores = dataStores ?? throw new ArgumentNullException(nameof(dataStores));
    }

    /// <summary>Decides whether <paramref name="caller"/> may start the pipeline's run with <paramref name="runRoles"/>.</summary>
    /// <param name="caller">The caller starting the run.</param>
    /// <param name="reach">What the pipeline reads and writes.</param>
    /// <param name="runRoles">The roles the caller named for the run.</param>
    /// <param name="logger">The endpoint's logger.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success, or a 400-class refusal naming the role or the resource and why.</returns>
    public async Task<IGenericResult> Authorize(
        IAuthenticationContext caller,
        PipelineReach reach,
        IReadOnlyList<string>? runRoles,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var chosen = RunRoleSelection.Chosen(runRoles);
        return RunRoleSelection.Refusal(caller, chosen) is { } reason
            ? Refuse(logger, reach, chosen, reason)
            : await Judge(reach, chosen, logger, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Decides whether a run a schedule fires may act with the roles the schedule recorded, for the pipeline as it is stored now.
    /// </summary>
    /// <remarks>
    /// Why the firing caller need not hold the roles: a schedule fires with no person behind it, from the scheduler's own
    /// service credential, which spans every tenant. Whether the schedule's creator held the roles was decided when the schedule
    /// was created or changed; what this decides is that the pipeline, as it stands now, is still reachable with them.
    /// </remarks>
    /// <param name="reach">What the pipeline reads and writes.</param>
    /// <param name="runRoles">The roles the schedule recorded.</param>
    /// <param name="logger">The endpoint's logger.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success, or a 400-class refusal naming the role or the resource and why.</returns>
    public async Task<IGenericResult> AuthorizeScheduled(
        PipelineReach reach,
        IReadOnlyList<string>? runRoles,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var chosen = RunRoleSelection.Chosen(runRoles);
        return chosen.Count == 0
            ? Refuse(logger, reach, chosen, "a run names at least one role to act with")
            : await Judge(reach, chosen, logger, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IGenericResult> Judge(PipelineReach reach, IReadOnlyList<string> chosen, ILogger logger, CancellationToken ct)
    {
        var granted = await _grants.Get(chosen, ct).ConfigureAwait(false);
        if (granted.IsFailure)
        {
            // The expansion's own reason is the refusal's; one that gives none is a fault, not a refusal.
            return granted.CurrentMessage is { Length: > 0 } reason
                ? Refuse(logger, reach, chosen, reason)
                : GenericResult.Failure(PipelineEndpointLog.RunRolesExpansionUnexplained(logger, reach.PipelineName, string.Join(", ", chosen)));
        }

        if (granted.Value is null)
            return GenericResult.Failure(PipelineEndpointLog.RunRolesExpansionEmpty(logger, reach.PipelineName, string.Join(", ", chosen)));

        var permissions = granted.Value;
        var dataSets = await ReachDataSets(reach, chosen, permissions, logger, ct).ConfigureAwait(false);
        if (dataSets.IsFailure)
            return dataSets;

        var source = await ReachSourceContainer(reach, chosen, permissions, logger, ct).ConfigureAwait(false);
        if (source.IsFailure)
            return source;

        var destination = await ReachDestinationContainer(reach, chosen, permissions, logger, ct).ConfigureAwait(false);
        return destination.IsFailure ? destination : await ReachContainers(reach, chosen, permissions, logger, ct).ConfigureAwait(false);
    }

    // Every data set the pipeline reads or writes -- its own source and destination, and what its stages and steps name.
    private async Task<IGenericResult> ReachDataSets(
        PipelineReach reach, IReadOnlyList<string> roles, IReadOnlyCollection<string> permissions, ILogger logger, CancellationToken ct)
    {
        foreach (var reached in DataSetsTouched(reach))
        {
            var refused = await ReachDataSet(reach, roles, permissions, reached, logger, ct).ConfigureAwait(false);
            if (refused.IsFailure)
                return refused;
        }

        return GenericResult.Success();
    }

    // The data set itself, then the connection and data store behind each of its sources: reading or writing the data set is
    // reading or writing what it sits on.
    private async Task<IGenericResult> ReachDataSet(
        PipelineReach reach, IReadOnlyList<string> roles, IReadOnlyCollection<string> permissions, ReachedDataSet reached, ILogger logger, CancellationToken ct)
    {
        var dataSet = await _dataSets.Get(reached.Name, ct).ConfigureAwait(false);
        if (!dataSet.IsSuccess || dataSet.Value is null)
            return Refuse(logger, reach, roles, $"data set '{reached.Name}' is not visible to the caller");

        var needed = reached.Writes ? DataSetWrite : DataSetRead;
        if (!PermissionCoverage.Covers(permissions, needed))
            return Refuse(logger, reach, roles, $"the roles do not carry {needed}, which {Verb(reached.Writes)} data set '{reached.Name}' needs");

        foreach (var source in dataSet.Value.Sources)
        {
            var connection = await ReachConnection(reach, roles, permissions, new ReachedConnection(source.ConnectionName, reached.Writes), logger, ct).ConfigureAwait(false);
            if (connection.IsFailure)
                return connection;

            if (string.IsNullOrWhiteSpace(source.DataStoreName))
                continue;

            var store = await ReachDataStore(reach, roles, permissions, source.DataStoreName, reached.Writes, logger, ct).ConfigureAwait(false);
            if (store.IsFailure)
                return store;
        }

        return GenericResult.Success();
    }

    private async Task<IGenericResult> ReachConnection(
        PipelineReach reach, IReadOnlyList<string> roles, IReadOnlyCollection<string> permissions, ReachedConnection reached, ILogger logger, CancellationToken ct)
    {
        var connection = await _connections.Get(reached.Name, ct).ConfigureAwait(false);
        if (!connection.IsSuccess || connection.Value is null)
            return Refuse(logger, reach, roles, $"connection '{reached.Name}' is not visible to the caller");

        var needed = reached.Writes ? ConnectionWrite : ConnectionRead;
        return PermissionCoverage.Covers(permissions, needed)
            ? GenericResult.Success()
            : Refuse(logger, reach, roles, $"the roles do not carry {needed}, which {Verb(reached.Writes)} connection '{reached.Name}' needs");
    }

    private async Task<IGenericResult> ReachDataStore(
        PipelineReach reach, IReadOnlyList<string> roles, IReadOnlyCollection<string> permissions, string name, bool writes, ILogger logger, CancellationToken ct)
    {
        var store = await _dataStores.Get(name, ct).ConfigureAwait(false);
        if (!store.IsSuccess || store.Value is null)
            return Refuse(logger, reach, roles, $"data store '{name}' is not visible to the caller");

        var needed = writes ? DataStoreWrite : DataStoreRead;
        return PermissionCoverage.Covers(permissions, needed)
            ? GenericResult.Success()
            : Refuse(logger, reach, roles, $"the roles do not carry {needed}, which {Verb(writes)} data store '{name}' needs");
    }

    // A data store container source (FDW-1103): the container must resolve under the caller's own row-level security, the store
    // must not be one the host declares for itself (those are the platform's, refused to every user request at the gateway
    // too), and the roles must carry datastores:read. All three parts are required, and a pipeline reads from one place.
    private async Task<IGenericResult> ReachSourceContainer(
        PipelineReach reach, IReadOnlyList<string> roles, IReadOnlyCollection<string> permissions, ILogger logger, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reach.SourceDataStoreName)
            && string.IsNullOrWhiteSpace(reach.SourceDataStorePath)
            && string.IsNullOrWhiteSpace(reach.SourceContainerName))
            return GenericResult.Success();

        if (string.IsNullOrWhiteSpace(reach.SourceDataStoreName)
            || string.IsNullOrWhiteSpace(reach.SourceDataStorePath)
            || string.IsNullOrWhiteSpace(reach.SourceContainerName))
            return Refuse(logger, reach, roles,
                "a data store source names its store, path and container together (SourceDataStoreName, SourceDataStorePath, SourceContainerName)");

        if (!string.IsNullOrWhiteSpace(reach.SourceDataSet))
            return Refuse(logger, reach, roles, "a pipeline reads from one place: name a source data set or a data store container, not both");

        return await ReachContainer(
            reach, roles, permissions, new ReachedContainer(reach.SourceDataStoreName, reach.SourceDataStorePath, reach.SourceContainerName, false), logger, ct)
            .ConfigureAwait(false);
    }

    // A data store container destination (FDW-1269), judged as the source is, but for writing: all three parts are named together, never
    // beside a destination data set or a destination connection, and the roles must carry datastores:write.
    private async Task<IGenericResult> ReachDestinationContainer(
        PipelineReach reach, IReadOnlyList<string> roles, IReadOnlyCollection<string> permissions, ILogger logger, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reach.DestinationDataStoreName)
            && string.IsNullOrWhiteSpace(reach.DestinationDataStorePath)
            && string.IsNullOrWhiteSpace(reach.DestinationContainerName))
            return GenericResult.Success();

        if (string.IsNullOrWhiteSpace(reach.DestinationDataStoreName)
            || string.IsNullOrWhiteSpace(reach.DestinationDataStorePath)
            || string.IsNullOrWhiteSpace(reach.DestinationContainerName))
            return Refuse(logger, reach, roles,
                "a data store destination names its store, path and container together (DestinationDataStoreName, DestinationDataStorePath, DestinationContainerName)");

        if (!string.IsNullOrWhiteSpace(reach.DestinationDataSet))
            return Refuse(logger, reach, roles, "a pipeline writes to one place: name a destination data set or a data store container, not both");

        return await ReachContainer(
            reach, roles, permissions, new ReachedContainer(reach.DestinationDataStoreName, reach.DestinationDataStorePath, reach.DestinationContainerName, true), logger, ct)
            .ConfigureAwait(false);
    }

    // The containers the pipeline's Execute steps write.
    private async Task<IGenericResult> ReachContainers(
        PipelineReach reach, IReadOnlyList<string> roles, IReadOnlyCollection<string> permissions, ILogger logger, CancellationToken ct)
    {
        foreach (var container in reach.Steps.Containers)
        {
            var refused = await ReachContainer(reach, roles, permissions, container, logger, ct).ConfigureAwait(false);
            if (refused.IsFailure)
                return refused;
        }

        return GenericResult.Success();
    }

    private async Task<IGenericResult> ReachContainer(
        PipelineReach reach, IReadOnlyList<string> roles, IReadOnlyCollection<string> permissions, ReachedContainer reached, ILogger logger, CancellationToken ct)
    {
        var store = await _dataStores.Get(reached.DataStore, ct).ConfigureAwait(false);
        if (!store.IsSuccess || store.Value is null)
            return Refuse(logger, reach, roles, $"data store '{reached.DataStore}' is not visible to the caller");
        if (store.Value.DeclaredByHost)
            return Refuse(logger, reach, roles, $"data store '{reached.DataStore}' belongs to the platform and is not a pipeline {(reached.Writes ? "destination" : "source")}");

        var container = await _dataStores.Get(reached.DataStore, reached.Path, reached.Container, ct).ConfigureAwait(false);
        if (!container.IsSuccess || container.Value is null)
            return Refuse(logger, reach, roles,
                $"container '{reached.Container}' in path '{reached.Path}' of data store '{reached.DataStore}' is not visible to the caller");

        var needed = reached.Writes ? DataStoreWrite : DataStoreRead;
        return PermissionCoverage.Covers(permissions, needed)
            ? GenericResult.Success()
            : Refuse(logger, reach, roles, $"the roles do not carry {needed}, which {Verb(reached.Writes)} data store '{reached.DataStore}' needs");
    }

    private static IEnumerable<ReachedDataSet> DataSetsTouched(PipelineReach reach) =>
        Named(reach.SourceDataSet).Select(name => new ReachedDataSet(name, false))
            .Concat(reach.Steps.DataSets)
            .Concat(Named(reach.DestinationDataSet).Select(name => new ReachedDataSet(name, true)))
            .Distinct();

    private static IEnumerable<string> Named(string? name)
        => string.IsNullOrWhiteSpace(name) ? [] : [name];

    private static string Verb(bool writes) => writes ? "writing" : "reading";

    private static IGenericResult Refuse(ILogger logger, PipelineReach reach, IReadOnlyList<string> roles, string reason)
        => GenericResult.Failure(EtlResultCodes.ByName("PipelineRunRefused"), logger,
            ResultDetails.Create("PipelineName", reach.PipelineName, "RunRoles", roles.Count == 0 ? "no role" : string.Join(", ", roles), "Reason", reason));
}
