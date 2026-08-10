using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Web.RestEndpoints.Extensions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Generic base endpoint for getting pipeline execution status.
/// Derived classes must implement the actual status retrieval logic and dependency injection.
/// </summary>
public abstract class GetPipelineStatusEndpointBase : Endpoint<GetPipelineStatusRequest, GetPipelineStatusResponse>
{
    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected virtual string ResourceName => "pipelines";

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Get($"/{ResourceName}/{{Name}}/status");
#if DEVELOP
        AllowAnonymous();
#else
        Policies($"{ResourceName}:read");
#endif
        Summary(s =>
        {
            s.Summary = "Get pipeline status";
            s.Description = "Retrieves the current status of a pipeline by name.";
        });
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (summary, tags, etc.).</summary>
    protected virtual void ConfigureEndpoint() { }

    /// <summary>
    /// Retrieves the pipeline status by name.
    /// </summary>
    /// <param name="request">The status request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The status response.</returns>
    protected abstract Task<GetPipelineStatusResponse> RetrieveStatus(GetPipelineStatusRequest request, CancellationToken ct);

    /// <summary>
    /// Handles the endpoint request.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="ct">Cancellation token.</param>
    public override async Task HandleAsync(GetPipelineStatusRequest request, CancellationToken ct)
    {
        var response = await RetrieveStatus(request, ct).ConfigureAwait(false);

        if (response.Found)
        {
            await Send.OkAsync(response, ct).ConfigureAwait(false);
        }
        else
        {
            await HttpContext.WriteNotFound("PipelineStatus", request.Name, ct).ConfigureAwait(false);
        }
    }
}
