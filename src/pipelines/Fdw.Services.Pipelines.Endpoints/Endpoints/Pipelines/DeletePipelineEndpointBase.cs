using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Thresholds;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;
using Fdw.Services.Pipelines.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Generic base endpoint for deleting a pipeline configuration.
/// </summary>
/// <typeparam name="TConfig">The pipeline implementation this endpoint serves.</typeparam>
public abstract class DeletePipelineEndpointBase<TConfig> : CrudDeleteEndpointBase<PipelineNameRequest>
    where TConfig : IPipelineImplementationConfiguration
{
    private readonly PipelineServiceConfigurationProvider _provider;
    private readonly WaitThresholdOwnerRelease _release;

    /// <inheritdoc />
    protected DeletePipelineEndpointBase(
        ILogger<DeletePipelineEndpointBase<TConfig>> logger, PipelineServiceConfigurationProvider provider, IConfigurationGatewayProvider gateways) : base(logger)
    {
        _provider = provider;
        _release = new WaitThresholdOwnerRelease(gateways, logger);
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "pipelines";

    /// <summary>Returns the pipeline name from the request.</summary>
    protected override string GetResourceIdentifier(PipelineNameRequest request) => request.Name;

    /// <summary>Checks whether the pipeline exists before deletion.</summary>
    protected override async Task<IGenericResult<bool>> CheckExistsForDelete(PipelineNameRequest request, CancellationToken ct)
    {
        var existingResult = await _provider.Get(request.Name, ct).ConfigureAwait(false);
        return GenericResult<bool>.Success(existingResult.IsSuccess && existingResult.Value != null);
    }

    /// <summary>Deletes the pipeline configuration via the DataGateway.</summary>
    protected override async Task<IGenericResult> Delete(PipelineNameRequest request, CancellationToken ct)
    {
        var existingResult = await _provider.Get(request.Name, ct).ConfigureAwait(false);
        var existing = existingResult.IsSuccess ? existingResult.Value : null;

        if (existing == null)
        {
            return GenericResult.Success();
        }

        // The pipeline's own level of thresholds on waiting runs goes with it, and a pipeline another owner's threshold counts is not deleted (FDW-3271).
        var released = await _release.ReleasePipeline(request.Name, ct).ConfigureAwait(false);
        if (released.IsFailure)
        {
            return released;
        }

        var deleteResult = await _provider.Delete(existing.Id, ct).ConfigureAwait(false);
        if (deleteResult.IsFailure)
        {
            return deleteResult;
        }

        return GenericResult.Success();
    }
}
