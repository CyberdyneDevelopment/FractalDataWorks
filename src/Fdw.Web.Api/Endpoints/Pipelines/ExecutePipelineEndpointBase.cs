using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Generic base endpoint for executing a pipeline.
/// Derived classes must implement the actual execution logic and dependency injection.
/// </summary>
public abstract class ExecutePipelineEndpointBase : Endpoint<ExecutePipelineRequest, ExecutePipelineResponse>
{
    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected virtual string ResourceName => "pipelines";

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Post($"/{ResourceName}/{{Name}}/execute");
#if DEVELOP
        AllowAnonymous();
#else
        Policies($"{ResourceName}:execute");
#endif
        Summary(s =>
        {
            s.Summary = "Execute a pipeline by name";
            s.Description = "Triggers execution of the specified pipeline.";
        });
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (summary, tags, etc.).</summary>
    protected virtual void ConfigureEndpoint() { }

    /// <summary>
    /// Executes the pipeline by name.
    /// </summary>
    /// <param name="request">The execution request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The execution response.</returns>
    protected abstract Task<ExecutePipelineResponse> PerformExecution(ExecutePipelineRequest request, CancellationToken ct);

    /// <summary>
    /// Handles the endpoint request.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="ct">Cancellation token.</param>
    public override async Task HandleAsync(ExecutePipelineRequest request, CancellationToken ct)
    {
        var response = await PerformExecution(request, ct).ConfigureAwait(false);

        if (response.Success)
        {
            await Send.OkAsync(response, ct).ConfigureAwait(false);
        }
        else if (response.NotFound)
        {
            await Send.ResponseAsync(response, StatusCodes.Status404NotFound, ct).ConfigureAwait(false);
        }
        else
        {
            await Send.ResponseAsync(response, StatusCodes.Status500InternalServerError, ct).ConfigureAwait(false);
        }
    }
}
