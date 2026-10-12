using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data;
using Fdw.Services.Etl;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.Etl.Transforms;
using Fdw.Services.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Generic base endpoint for updating an existing pipeline configuration.
/// </summary>
/// <typeparam name="TConfig">The pipeline implementation this endpoint serves.</typeparam>
public abstract class UpdatePipelineEndpointBase<TConfig> : CrudUpdateEndpointBase<UpdatePipelineRequest, PipelineDetailResponse>
    where TConfig : IPipelineImplementationConfiguration
{
    private readonly PipelineServiceConfigurationProvider _provider;
    private readonly PipelineLoadDraft _loadDraft;
    private readonly StageChainLookup _chains;
    private readonly ITransformationConfigurationProvider _transformations;

    /// <summary>Initializes a new instance of the <see cref="UpdatePipelineEndpointBase{TConfig}"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="provider">Reads and saves pipelines.</param>
    /// <param name="dataSets">Resolves the pipeline's data sets as the caller.</param>
    /// <param name="connections">Resolves the pipeline's connections as the caller.</param>
    /// <param name="dataStoreConfigurations">Resolves the data stores an Execute load step's container is read from.</param>
    /// <param name="transformations">Resolves the named Transformations a field mapping's chain applies.</param>
    protected UpdatePipelineEndpointBase(
        ILogger<UpdatePipelineEndpointBase<TConfig>> logger,
        PipelineServiceConfigurationProvider provider,
        IDataSetConfigurationProvider dataSets,
        IConnectionConfigurationProvider connections,
        DataStoreConfigurationProvider dataStoreConfigurations,
        ITransformationConfigurationProvider transformations) : base(logger)
    {
        _provider = provider;
        _loadDraft = new PipelineLoadDraft(dataSets, dataStoreConfigurations, connections, logger);
        _chains = new StageChainLookup(transformations, logger);
        _transformations = transformations;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "pipelines";

    /// <summary>Returns the pipeline name from the update request.</summary>
    protected override string GetResourceIdentifier(UpdatePipelineRequest request) => request.Name;

    /// <summary>Loads the existing pipeline configuration by name.</summary>
    protected override async Task<IGenericResult<PipelineDetailResponse?>> FindForUpdate(UpdatePipelineRequest request, CancellationToken ct)
    {
        var existingResult = await _provider.Get(request.Name, ct).ConfigureAwait(false);

        if (!existingResult.IsSuccess || existingResult.Value == null)
        {
            return GenericResult<PipelineDetailResponse?>.Success(null);
        }

        return GenericResult<PipelineDetailResponse?>.Success(PipelineDetailMapper.Map(existingResult.Value));
    }

    /// <summary>
    /// Gets the transforms mapped from the current request's <see cref="UpdatePipelineRequest.Transforms"/>
    /// by <see cref="Update"/>, populated before <see cref="ApplyUpdates"/> is invoked. Null means the
    /// request did not supply a <c>Transforms</c> list (leave the existing set unchanged); a non-null
    /// (including empty) list is the fully-mapped replacement set. Concrete subclasses read this to update
    /// their typed body's <c>Transforms</c> collection — the mapping and its fail-loud validation are owned
    /// entirely by this FDW base class (FDW-556).
    /// </summary>
    protected IReadOnlyList<PipelineTransformConfiguration>? MappedTransforms { get; private set; }

    /// <summary>
    /// The load definition the saved pipeline will carry (FDW-1110): what the request states, or the stored definition
    /// where it states nothing, judged as a whole. The concrete endpoint writes it onto the engine's record; it is set
    /// before <see cref="ApplyUpdates"/> runs.
    /// </summary>
    protected MappedPipelineLoad EffectiveLoad { get; private set; } = new(null, null, null);

    /// <summary>Updates the pipeline configuration and persists it via the DataGateway.</summary>
    protected override async Task<IGenericResult<PipelineDetailResponse>> Update(UpdatePipelineRequest request, PipelineDetailResponse existing, CancellationToken ct)
    {
        var originalResult = await _provider.Get(request.Name, ct).ConfigureAwait(false);

        if (!originalResult.IsSuccess || originalResult.Value == null)
        {
            return GenericResult<PipelineDetailResponse>.Failure(ServicesResultCodes.ByName("ConfigurationNotFound"));
        }

        var mapped = await MapBody(request, originalResult.Value, existing, ct).ConfigureAwait(false);
        if (!mapped.IsSuccess)
        {
            return mapped.ToNewResult<PipelineDetailResponse>();
        }

        var updatedResult = ApplyUpdates(request, (TConfig)originalResult.Value);
        if (!updatedResult.IsSuccess)
        {
            return updatedResult.ToNewResult<PipelineDetailResponse>();
        }

        var updated = updatedResult.Value!;

        var saveResult = await _provider.Save(updated, "Pipeline", updated.Implementation, updated.Name, ct).ConfigureAwait(false);
        if (saveResult.IsFailure)
        {
            return saveResult.ToNewResult<PipelineDetailResponse>();
        }

        return await PipelineDetailMapper.Read(_provider, _transformations, updated.Name, ct).ConfigureAwait(false);
    }

    // The transforms are mapped when the request states them, and the load definition the saved pipeline will carry is
    // judged, before anything is written.
    private async Task<IGenericResult> MapBody(
        UpdatePipelineRequest request, IPipelineImplementationConfiguration original, PipelineDetailResponse existing, CancellationToken ct)
    {
        if (request.Transforms != null)
        {
            var chains = await _chains.Resolve(request.Name, request.Transforms, ct).ConfigureAwait(false);
            if (chains.Refusal<List<PipelineTransformConfiguration>>(Logger) is { } refused)
            {
                return refused;
            }

            var transformsResult = PipelineTransformConfigurationMapper.Map(existing.PipelineType, request.Transforms, Logger, chains.Ids);
            if (!transformsResult.IsSuccess)
            {
                return transformsResult;
            }

            MappedTransforms = transformsResult.Value;

            var notifies = await PipelineNotifyCheck.Check(MappedTransforms!, TryResolve<IRuleNotifier>(), Logger, ct).ConfigureAwait(false);
            if (!notifies.IsSuccess)
            {
                return notifies;
            }
        }

        var loaded = await JudgeLoad(request, original, existing, ct).ConfigureAwait(false);
        if (!loaded.IsSuccess)
        {
            return loaded;
        }

        EffectiveLoad = loaded.Value!;

        var truncate = PipelineTruncateConsistency.Check(request.TruncateBeforeLoad, EffectiveLoad.LoadMode, Logger);
        return truncate is null ? GenericResult.Success() : PipelineLoadRefusal.Of(truncate.Message);
    }

    // Contract v3.1 B: loadMode, loadOptions, loadSteps and valueSources are ONE UNIT. A body that states none of the four
    // leaves the stored unit alone; a body that states any replaces it whole with what the body says (what it leaves out is
    // cleared). The run parameters are their own: stated replaces, absent keeps. The result is judged as a whole.
    private async Task<IGenericResult<MappedPipelineLoad>> JudgeLoad(
        UpdatePipelineRequest request, IPipelineImplementationConfiguration original, PipelineDetailResponse existing, CancellationToken ct)
    {
        if (!PipelineLoadPatch.Changes(request))
        {
            return GenericResult<MappedPipelineLoad>.Success(PipelineLoadPatch.Stored(original));
        }

        var stated = _loadDraft.Map(request);
        if (!stated.IsSuccess)
        {
            return stated;
        }

        var stored = PipelineLoadPatch.Stored(original);
        var effective = PipelineLoadPatch.Apply(request, stated.Value!, stored);
        return await _loadDraft.Judge(effective, PipelineLoadPatch.EffectiveDestinationDataSet(request, existing), PipelineLoadPatch.Expressions(request, original), ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Applies updates from the request to the existing configuration. Override for type-specific fields;
    /// read <see cref="MappedTransforms"/> (when non-null) to replace the typed body's <c>Transforms</c>.
    /// An update the application cannot apply (e.g. to an engine it does not wire) is a failure result.
    /// </summary>
    protected abstract IGenericResult<TConfig> ApplyUpdates(UpdatePipelineRequest request, TConfig originalConfig);
}
