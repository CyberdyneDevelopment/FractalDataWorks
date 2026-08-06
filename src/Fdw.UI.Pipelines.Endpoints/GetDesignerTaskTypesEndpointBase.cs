using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.UI.Pipelines.Clients.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.Pipelines.Endpoints;

/// <summary>
/// Base endpoint for retrieving all available task types for the designer palette.
/// Route: GET /pipelines/designer/task-types
/// </summary>
public abstract class GetDesignerTaskTypesEndpointBase : EndpointWithoutRequest<IReadOnlyList<TaskTypeInfo>>
{
    /// <summary>Gets the authorization policy name for read operations.</summary>
    protected virtual string ReadPolicy => "pipelines:read";

    /// <summary>Gets the logger instance. Resolved during HandleAsync.</summary>
    protected new ILogger Logger { get; private set; } = null!;

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Get("/pipelines/designer/task-types");
#if DEVELOP
        AllowAnonymous();
#else
        Policies(ReadPolicy);
#endif
        Summary(s =>
        {
            s.Summary = "Get available pipeline task types";
            s.Description = "Returns all task types available in the designer palette, including port counts and configurable properties.";
        });
    }

    /// <summary>Returns all available task types for the designer palette.</summary>
    public override async Task HandleAsync(CancellationToken ct)
    {
        Logger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        var taskTypes = await LoadTaskTypes(ct).ConfigureAwait(false);
        await Send.OkAsync(taskTypes, ct).ConfigureAwait(false);
    }

    /// <summary>Loads all available task type descriptors. Implement to enumerate registered task types.</summary>
    protected abstract Task<IReadOnlyList<TaskTypeInfo>> LoadTaskTypes(CancellationToken ct);
}
