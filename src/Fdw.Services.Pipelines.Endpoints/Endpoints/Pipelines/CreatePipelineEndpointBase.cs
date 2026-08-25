using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Transforms;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Generic base endpoint for creating a new pipeline configuration.
/// </summary>
/// <typeparam name="TConfig">The concrete pipeline configuration type.</typeparam>
public abstract class CreatePipelineEndpointBase<TConfig> : CrudCreateEndpointBase<CreatePipelineRequest, PipelineDetailResponse>
    where TConfig : PipelineConfiguration
{
    // Why: PipelineServiceConfigurationProvider replaces IOptionsMonitor<List<T>> with dual-source
    // (ctrl + cfg) provider. Pipelines are excluded from MsSqlConfigurationSource (FDW-220/221/222),
    // so all pipeline configs come from the the provider.s user cache (cfg schema).
    private readonly PipelineServiceConfigurationProvider _provider;

    /// <inheritdoc />
    protected CreatePipelineEndpointBase(PipelineServiceConfigurationProvider provider)
    {
        _provider = provider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "pipelines";

    /// <summary>Returns the pipeline name from the create request.</summary>
    protected override string GetResourceName(CreatePipelineRequest request) => request.Name;

    /// <summary>Checks whether a pipeline with the requested name already exists.</summary>
    protected override async Task<IGenericResult<bool>> CheckExists(CreatePipelineRequest request, CancellationToken ct)
    {
        var existingResult = await _provider.Get(request.Name, ct).ConfigureAwait(false);
        return GenericResult<bool>.Success(existingResult.IsSuccess && existingResult.Value != null);
    }

    /// <summary>
    /// Gets the transforms mapped from the current request's <see cref="CreatePipelineRequest.Transforms"/>
    /// by <see cref="Create"/>, populated before <see cref="CreateConfiguration"/> is invoked. Concrete
    /// subclasses read this to populate their typed body's <c>Transforms</c> collection — the mapping
    /// (per-option dispatch via <c>TransformTypes</c>, never a switch on operation type) and its fail-loud
    /// validation are owned entirely by this FDW base class, not duplicated per application (FDW-556).
    /// </summary>
    protected IReadOnlyList<PipelineTransformConfiguration> MappedTransforms { get; private set; } = [];

    /// <summary>Creates the pipeline configuration and persists it via the DataGateway.</summary>
    protected override async Task<IGenericResult<PipelineDetailResponse>> Create(CreatePipelineRequest request, CancellationToken ct)
    {
        // Why: dispatch every transform spec through TransformTypes.ByName(...).MapSpecToConfiguration
        // BEFORE building the concrete configuration — a param-less combine op (Aggregate/Lookup/
        // Calculate/Filter) must fail the create call loudly, never silently persist an inert transform.
        var transformsResult = PipelineTransformConfigurationMapper.Map(request.Transforms, Logger);
        if (!transformsResult.IsSuccess)
        {
            return transformsResult.ToNewResult<PipelineDetailResponse>();
        }

        MappedTransforms = transformsResult.Value!;

        // Why: API contract — IDs are uuid v7 for time-orderable persistence.
        var pipelineId = Guid.CreateVersion7();
        var config = CreateConfiguration(request, pipelineId);

        var saveResult = await _provider.Save(config, ct).ConfigureAwait(false);
        if (saveResult.IsFailure)
        {
            return saveResult.ToNewResult<PipelineDetailResponse>();
        }

        return GenericResult<PipelineDetailResponse>.Success(MapToDetail(config, request, pipelineId));
    }

    /// <summary>
    /// Builds a concrete pipeline configuration from the create request. Override for type-specific fields;
    /// read <see cref="MappedTransforms"/> to populate the typed body's <c>Transforms</c> collection.
    /// </summary>
    protected abstract TConfig CreateConfiguration(CreatePipelineRequest request, Guid pipelineId);

    /// <summary>Maps the saved configuration to a detail DTO. Override for type-specific fields.</summary>
    protected abstract PipelineDetailResponse MapToDetail(TConfig savedConfig, CreatePipelineRequest request, Guid pipelineId);

    /// <summary>Sends a 201 Created response with the pipeline detail.</summary>
    protected override Task SendCreatedResponse(PipelineDetailResponse detail, CancellationToken ct)
    {
        return Send.ResponseAsync(detail, 201, ct);
    }
}
