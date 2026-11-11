using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Pipelines.Loads;

/// <summary>
/// Resolves the data set or container a load writes to, or an insert-from-query reads, to a container and the connection
/// that serves it (FDW-1109), the way the data set dispatch resolves the single source it writes to.
/// </summary>
/// <remarks>
/// Why it is not the data gateway: a unit of work runs on one connection's transaction, so the engine needs the
/// connection and the container itself, not a gateway call. It resolves the same way the dispatch does: the data set's one
/// source names a data store, path and container, and the container's data store names its connection. A data set with
/// more or fewer than one source cannot be written to, and is refused.
/// </remarks>
public sealed class LoadTargetResolver
{
    private readonly IDataSetConfigurationProvider? _dataSets;
    private readonly IDataStoreProvider? _dataStores;
    private readonly ILogger<LoadTargetResolver> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoadTargetResolver"/> class.
    /// </summary>
    /// <param name="dataSets">The data set configuration provider, or <see langword="null"/> when the pipeline was built without one.</param>
    /// <param name="dataStores">The data store provider, or <see langword="null"/> when the pipeline was built without one.</param>
    /// <param name="logger">The logger.</param>
    public LoadTargetResolver(
        IDataSetConfigurationProvider? dataSets,
        IDataStoreProvider? dataStores,
        ILogger<LoadTargetResolver>? logger = null)
    {
        _dataSets = dataSets;
        _dataStores = dataStores;
        _logger = logger ?? NullLogger<LoadTargetResolver>.Instance;
    }

    /// <summary>Resolves a data set to its one source's container.</summary>
    /// <param name="owner">What the load is, for refusal messages.</param>
    /// <param name="dataSetName">The data set.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The target, or a failure saying why the data set cannot be used.</returns>
    public async Task<IGenericResult<LoadTarget>> ForDataSet(string owner, string dataSetName, CancellationToken cancellationToken = default)
    {
        if (_dataSets is null || _dataStores is null)
            return GenericResult<LoadTarget>.Failure(PipelineRunLog.LoadServiceMissing(_logger, owner, "data set and data store providers"));

        var set = await _dataSets.Get(dataSetName, cancellationToken).ConfigureAwait(false);
        if (set.IsFailure)
            return set.ToNewResult<LoadTarget>();

        var sources = set.Value!.Sources;
        if (sources.Count != 1)
            return GenericResult<LoadTarget>.Failure(PipelineRunLog.LoadTargetUnusable(
                _logger, owner, dataSetName, $"a data set a load reads or writes has exactly one source, and this one has {sources.Count}"));

        var source = sources[0];
        if (string.IsNullOrWhiteSpace(source.ContainerName))
            return GenericResult<LoadTarget>.Failure(PipelineRunLog.LoadTargetUnusable(_logger, owner, dataSetName, "its source names no container"));

        return await Resolve(owner, dataSetName, source.DataStoreName, source.PathValue, source.ContainerName, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Resolves a data store, path and container.</summary>
    /// <param name="owner">What the load is, for refusal messages.</param>
    /// <param name="dataStoreName">The data store.</param>
    /// <param name="pathName">The path in the data store; empty when the container is addressed without one.</param>
    /// <param name="containerName">The container.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The target, or a failure saying why the container cannot be used.</returns>
    public Task<IGenericResult<LoadTarget>> ForContainer(
        string owner,
        string dataStoreName,
        string pathName,
        string containerName,
        CancellationToken cancellationToken = default)
        => _dataStores is null
            ? Task.FromResult<IGenericResult<LoadTarget>>(GenericResult<LoadTarget>.Failure(
                PipelineRunLog.LoadServiceMissing(_logger, owner, "data store provider")))
            : Resolve(owner, string.Concat(dataStoreName, "/", containerName), dataStoreName, pathName, containerName, cancellationToken);

    /// <summary>
    /// Resolves what a load unit writes to (FDW-3193): the data set or container it names, and for the File kind that container at the file the unit
    /// states. The Files kind resolves to the declared container; the executor moves it once for each value of the split field.
    /// </summary>
    /// <param name="unit">The load.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The target, or a failure saying why the unit cannot be written to.</returns>
    public async Task<IGenericResult<LoadTarget>> ForUnit(LoadUnit unit, CancellationToken cancellationToken = default)
    {
        var declared = unit.DataSet is not null
            ? await ForDataSet(unit.Owner, unit.DataSet, cancellationToken).ConfigureAwait(false)
            : await ForContainer(unit.Owner, unit.DataStore!, unit.Path!, unit.Container!, cancellationToken).ConfigureAwait(false);
        if (declared.IsFailure || unit.Addressing is not { } addressing)
            return declared;

        if (declared.Value!.Container.Path is not IFilePath)
            return GenericResult<LoadTarget>.Failure(PipelineRunLog.TargetNotAFile(_logger, unit.Owner, declared.Value.Description));

        return addressing.Splits
            ? declared
            : declared.Value.Readdressed(unit.Owner, _logger, addressing.PhysicalPath, addressing.PhysicalName);
    }

    /// <summary>
    /// Resolves the stage container of a load that stages its keys (FDW-4956): the container the load's row names, in the target's own data store and
    /// path. It must sit on the target's connection, so a batch's keys commit in the same transaction as its rows, and it must declare the key columns
    /// and the stage key column, because an insert drops a value its container does not declare.
    /// </summary>
    /// <param name="unit">The load; it names its stage container and stage key column.</param>
    /// <param name="target">The load's resolved target.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The stage, or a failure saying why it cannot be used.</returns>
    public async Task<IGenericResult<LoadTarget>> ForStage(LoadUnit unit, LoadTarget target, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(unit.Row.StageContainer) || string.IsNullOrWhiteSpace(unit.Row.StageKeyColumn))
            return GenericResult<LoadTarget>.Failure(PipelineRunLog.StageNotNamed(_logger, unit.Owner));

        var stage = unit.DataSet is not null
            ? await StageBesideDataSet(unit.Owner, unit.DataSet, unit.Row.StageContainer, cancellationToken).ConfigureAwait(false)
            : await ForContainer(unit.Owner, unit.DataStore!, unit.Path!, unit.Row.StageContainer, cancellationToken).ConfigureAwait(false);
        if (stage.IsFailure)
            return stage;
        if (stage.Value!.ConnectionId != target.ConnectionId)
            return GenericResult<LoadTarget>.Failure(PipelineRunLog.StageOnAnotherConnection(_logger, unit.Owner, stage.Value.Description, target.Description));

        var declared = stage.Value.Container.Schema.Fields;
        var missing = LoadColumns.Keys(unit.Row).Append(unit.Row.StageKeyColumn)
            .FirstOrDefault(column => !declared.Any(field => string.Equals(field.Name, column, StringComparison.OrdinalIgnoreCase)));
        return missing is null
            ? stage
            : GenericResult<LoadTarget>.Failure(PipelineRunLog.StageColumnNotDeclared(_logger, unit.Owner, stage.Value.Description, missing));
    }

    // The stage of a data set load sits in the data store and path of the data set's one source (FDW-4956).
    private async Task<IGenericResult<LoadTarget>> StageBesideDataSet(string owner, string dataSetName, string stageContainer, CancellationToken cancellationToken)
    {
        if (_dataSets is null || _dataStores is null)
            return GenericResult<LoadTarget>.Failure(PipelineRunLog.LoadServiceMissing(_logger, owner, "data set and data store providers"));

        var set = await _dataSets.Get(dataSetName, cancellationToken).ConfigureAwait(false);
        if (set.IsFailure)
            return set.ToNewResult<LoadTarget>();

        var sources = set.Value!.Sources;
        if (sources.Count != 1)
            return GenericResult<LoadTarget>.Failure(PipelineRunLog.LoadTargetUnusable(
                _logger, owner, dataSetName, $"a data set a load reads or writes has exactly one source, and this one has {sources.Count}"));

        var source = sources[0];
        return await Resolve(owner, string.Concat(source.DataStoreName, "/", stageContainer), source.DataStoreName, source.PathValue, stageContainer, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<IGenericResult<LoadTarget>> Resolve(
        string owner,
        string description,
        string dataStoreName,
        string pathName,
        string containerName,
        CancellationToken cancellationToken)
    {
        var container = await _dataStores!.Get(dataStoreName, pathName, containerName, cancellationToken).ConfigureAwait(false);
        if (container.IsFailure)
            return container.ToNewResult<LoadTarget>();

        return container.Value!.Parent is { Store.ConnectionId: var connectionId } && connectionId != Guid.Empty
            ? GenericResult<LoadTarget>.Success(new LoadTarget(description, container.Value, connectionId))
            : GenericResult<LoadTarget>.Failure(PipelineRunLog.LoadTargetUnusable(_logger, owner, description, "its data store names no connection"));
    }
}
