using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data;
using Fdw.Services.Etl.Transforms;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Generic base endpoint for creating a new pipeline configuration.
/// </summary>
/// <typeparam name="TConfig">The pipeline implementation this endpoint serves.</typeparam>
public abstract class CreatePipelineEndpointBase<TConfig> : CrudCreateEndpointBase<CreatePipelineRequest, PipelineDetailResponse>
    where TConfig : IPipelineImplementationConfiguration
{
    private readonly PipelineServiceConfigurationProvider _provider;
    private readonly PipelineLoadDraft _loadDraft;
    private readonly StageChainLookup _chains;
    private readonly ITransformationConfigurationProvider _transformations;

    /// <summary>Initializes a new instance of the <see cref="CreatePipelineEndpointBase{TConfig}"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="provider">Reads and saves pipelines.</param>
    /// <param name="dataSets">Resolves the pipeline's data sets as the caller.</param>
    /// <param name="connections">Resolves the pipeline's connections as the caller.</param>
    /// <param name="dataStoreConfigurations">Resolves the data stores an Execute load step's container is read from.</param>
    /// <param name="transformations">Resolves the named Transformations a field mapping's chain applies.</param>
    protected CreatePipelineEndpointBase(
        ILogger<CreatePipelineEndpointBase<TConfig>> logger,
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

    /// <summary>Returns the pipeline name from the create request.</summary>
    protected override string GetResourceName(CreatePipelineRequest request) => request.Name;

    /// <summary>Checks whether a pipeline with the requested name already exists.</summary>
    protected override async Task<IGenericResult<bool>> CheckExists(CreatePipelineRequest request, CancellationToken ct)
    {
        return GenericResult<bool>.Success(await PipelineDraft.NameTaken(_provider, request.Name, ct).ConfigureAwait(false));
    }

    /// <summary>
    /// Gets the transforms mapped from the current request's <see cref="CreatePipelineRequest.Transforms"/>
    /// by <see cref="Create"/>, populated before <see cref="CreateConfiguration"/> is invoked. Concrete
    /// subclasses read this to populate their typed body's <c>Transforms</c> collection — the mapping
    /// (per-option dispatch via <c>TransformTypes</c>, never a switch on operation type) and its fail-loud
    /// validation are owned entirely by this FDW base class, not duplicated per application (FDW-556).
    /// </summary>
    protected IReadOnlyList<PipelineTransformConfiguration> MappedTransforms { get; private set; } = [];

    /// <summary>The request's load definition as configuration rows, set once <see cref="Create"/> has judged it; the concrete endpoint writes it onto the engine's record.</summary>
    protected MappedPipelineLoad MappedLoad { get; private set; } = new(null, null, null);

    /// <summary>Creates the pipeline configuration and persists it via the DataGateway.</summary>
    protected override async Task<IGenericResult<PipelineDetailResponse>> Create(CreatePipelineRequest request, CancellationToken ct)
    {
        var mapped = await MapBody(request, ct).ConfigureAwait(false);
        if (!mapped.IsSuccess)
        {
            return mapped.ToNewResult<PipelineDetailResponse>();
        }

        var pipelineId = Guid.CreateVersion7();
        var configResult = CreateConfiguration(request, pipelineId);
        if (!configResult.IsSuccess)
        {
            return configResult.ToNewResult<PipelineDetailResponse>();
        }

        var config = configResult.Value!;

        var saveResult = await _provider.Save(config, "Pipeline", config.Implementation, config.Name, ct).ConfigureAwait(false);
        if (saveResult.IsFailure)
        {
            return saveResult.ToNewResult<PipelineDetailResponse>();
        }

        return await PipelineDetailMapper.Read(_provider, _transformations, config.Name, ct).ConfigureAwait(false);
    }

    // The transforms are mapped, and the load definition judged, before anything is written (FDW-1110): its structure by
    // the engine's own validator, its columns, containers and parameter references against the schemas it touches.
    private async Task<IGenericResult> MapBody(CreatePipelineRequest request, CancellationToken ct)
    {
        var chains = await _chains.Resolve(request.Name, request.Transforms, ct).ConfigureAwait(false);
        if (chains.Refusal<List<PipelineTransformConfiguration>>(Logger) is { } refused)
        {
            return refused;
        }

        var transformsResult = PipelineTransformConfigurationMapper.Map(request.PipelineType, request.Transforms, Logger, chains.Ids);
        if (!transformsResult.IsSuccess)
        {
            return transformsResult;
        }

        MappedTransforms = transformsResult.Value!;

        var notifies = await PipelineNotifyCheck.Check(MappedTransforms, TryResolve<IRuleNotifier>(), Logger, ct).ConfigureAwait(false);
        if (!notifies.IsSuccess)
        {
            return notifies;
        }

        var load = await _loadDraft.Prepare(request, request.DestinationDataSet, PipelineDraft.TransformExpressions(request.Transforms), ct).ConfigureAwait(false);
        if (!load.IsSuccess)
        {
            return load;
        }

        MappedLoad = load.Value!;

        var truncate = PipelineTruncateConsistency.Check(request.TruncateBeforeLoad, MappedLoad.LoadMode, Logger);
        return truncate is null ? GenericResult.Success() : PipelineLoadRefusal.Of(truncate.Message);
    }

    /// <summary>
    /// Builds a concrete pipeline configuration from the create request. Override for type-specific fields;
    /// read <see cref="MappedTransforms"/> to populate the typed body's <c>Transforms</c> collection.
    /// A request the application cannot build (e.g. an engine it does not wire) is a failure result.
    /// </summary>
    protected abstract IGenericResult<TConfig> CreateConfiguration(CreatePipelineRequest request, Guid pipelineId);

    /// <summary>Sends a 201 Created response with the pipeline detail.</summary>
    protected override Task SendCreatedResponse(PipelineDetailResponse detail, CancellationToken ct)
    {
        return Send.ResponseAsync(detail, 201, ct);
    }
}
