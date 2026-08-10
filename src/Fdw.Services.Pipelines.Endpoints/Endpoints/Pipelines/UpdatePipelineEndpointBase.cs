using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Transforms;
using Fdw.Services.Results;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Generic base endpoint for updating an existing pipeline configuration.
/// </summary>
/// <typeparam name="TConfig">The concrete pipeline configuration type.</typeparam>
public abstract class UpdatePipelineEndpointBase<TConfig> : CrudUpdateEndpoint<UpdatePipelineRequest, PipelineDetailResponse>
    where TConfig : PipelineConfiguration
{
    // Why: PipelineServiceConfigurationProvider replaces IOptionsMonitor<List<T>> with dual-source
    // (ctrl + cfg) provider for pipeline configuration management.
    private readonly PipelineServiceConfigurationProvider _provider;

    /// <inheritdoc />
    protected UpdatePipelineEndpointBase(PipelineServiceConfigurationProvider provider)
    {
        _provider = provider;
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

        var detail = MapToDetail((TConfig)existingResult.Value);
        return GenericResult<PipelineDetailResponse?>.Success(detail);
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

    /// <summary>Updates the pipeline configuration and persists it via the DataGateway.</summary>
    protected override async Task<IGenericResult<PipelineDetailResponse>> Update(UpdatePipelineRequest request, PipelineDetailResponse existing, CancellationToken ct)
    {
        // Why: dispatch every transform spec through TransformTypes.ByName(...).MapSpecToConfiguration
        // BEFORE applying updates — a param-less combine op must fail the update call loudly, never
        // silently persist an inert transform. A null Transforms list means "don't touch"; skip mapping.
        if (request.Transforms != null)
        {
            var transformsResult = PipelineTransformConfigurationMapper.Map(request.Transforms, Logger);
            if (!transformsResult.IsSuccess)
            {
                return transformsResult.ToNewResult<PipelineDetailResponse>();
            }

            MappedTransforms = transformsResult.Value;
        }

        var originalResult = await _provider.Get(request.Name, ct).ConfigureAwait(false);

        if (!originalResult.IsSuccess || originalResult.Value == null)
        {
            return GenericResult<PipelineDetailResponse>.Failure(ServicesResultCodes.ByName("ConfigurationNotFound"));
        }

        var updated = ApplyUpdates(request, (TConfig)originalResult.Value);

        var saveResult = await _provider.Save(updated, ct).ConfigureAwait(false);
        if (saveResult.IsFailure)
        {
            return saveResult.ToNewResult<PipelineDetailResponse>();
        }

        return GenericResult<PipelineDetailResponse>.Success(MapToDetail(updated));
    }

    /// <summary>
    /// Applies updates from the request to the existing configuration. Override for type-specific fields;
    /// read <see cref="MappedTransforms"/> (when non-null) to replace the typed body's <c>Transforms</c>.
    /// </summary>
    protected abstract TConfig ApplyUpdates(UpdatePipelineRequest request, TConfig originalConfig);

    /// <summary>Maps the updated configuration to a detail DTO. Override for type-specific fields.</summary>
    protected abstract PipelineDetailResponse MapToDetail(TConfig config);
}
