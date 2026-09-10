using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;
using Fdw.Services.Pipelines.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Generic base endpoint for getting a pipeline by name.
/// </summary>
/// <typeparam name="TConfig">The pipeline implementation this endpoint serves.</typeparam>
public abstract class GetPipelineEndpointBase<TConfig> : CrudGetEndpointBase<PipelineNameRequest, PipelineDetailResponse>
    where TConfig : IPipelineImplementationConfiguration
{
    private readonly PipelineServiceConfigurationProvider _provider;

    /// <inheritdoc />
    protected GetPipelineEndpointBase(ILogger<GetPipelineEndpointBase<TConfig>> logger, PipelineServiceConfigurationProvider provider) : base(logger)
    {
        _provider = provider;
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

        var detail = MapToDetail((TConfig)configResult.Value);
        return GenericResult<PipelineDetailResponse?>.Success(detail);
    }

    /// <summary>Maps a pipeline configuration to a detail DTO. Override for type-specific fields.</summary>
    protected abstract PipelineDetailResponse MapToDetail(TConfig config);
}
