using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Generic base endpoint for deleting a pipeline configuration.
/// </summary>
/// <typeparam name="TConfig">The concrete pipeline configuration type.</typeparam>
public abstract class DeletePipelineEndpointBase<TConfig> : CrudDeleteEndpointBase<PipelineNameRequest>
    where TConfig : PipelineConfiguration
{
    // Why: PipelineServiceConfigurationProvider replaces IOptionsMonitor<List<T>> with dual-source
    // (ctrl + cfg) provider for pipeline configuration management.
    private readonly PipelineServiceConfigurationProvider _provider;

    /// <inheritdoc />
    protected DeletePipelineEndpointBase(PipelineServiceConfigurationProvider provider)
    {
        _provider = provider;
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

        var deleteResult = await _provider.Delete(existing.Id, ct).ConfigureAwait(false);
        if (deleteResult.IsFailure)
        {
            return deleteResult;
        }

        return GenericResult.Success();
    }
}
