using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Transformations.Configuration;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Generic base endpoint for getting a pipeline by name.
/// </summary>
/// <typeparam name="TConfig">The pipeline implementation this endpoint serves.</typeparam>
public abstract class GetPipelineEndpointBase<TConfig> : CrudGetEndpointBase<PipelineNameRequest, PipelineDetailResponse>
    where TConfig : IPipelineImplementationConfiguration
{
    private readonly PipelineServiceConfigurationProvider _provider;
    private readonly ITransformationConfigurationProvider _transformations;

    /// <summary>Initializes a new instance of the <see cref="GetPipelineEndpointBase{TConfig}"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="provider">Reads the pipeline.</param>
    /// <param name="transformations">Reads the names of the Transformations the pipeline's field mappings chain.</param>
    protected GetPipelineEndpointBase(
        ILogger<GetPipelineEndpointBase<TConfig>> logger,
        PipelineServiceConfigurationProvider provider,
        ITransformationConfigurationProvider transformations) : base(logger)
    {
        _provider = provider;
        _transformations = transformations;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "pipelines";

    /// <summary>Returns the pipeline name from the request.</summary>
    protected override string GetResourceIdentifier(PipelineNameRequest request) => request.Name;

    /// <summary>Loads the pipeline configuration by name and maps it to a detail DTO.</summary>
    protected override async Task<IGenericResult<PipelineDetailResponse?>> FindByIdentifier(PipelineNameRequest request, CancellationToken ct)
    {
        var configResult = await _provider.Get(request.Name, ct).ConfigureAwait(false);

        if (!configResult.IsSuccess || configResult.Value == null)
        {
            return GenericResult<PipelineDetailResponse?>.Success(null);
        }

        var detail = await PipelineDetailMapper.Detail(configResult.Value, _transformations, ct).ConfigureAwait(false);
        return detail.IsSuccess
            ? GenericResult<PipelineDetailResponse?>.Success(detail.Value)
            : detail.ToNewResult<PipelineDetailResponse?>();
    }
}
