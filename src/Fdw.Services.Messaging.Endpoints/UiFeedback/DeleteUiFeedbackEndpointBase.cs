using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Messaging.Endpoints.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Messaging.Endpoints;

/// <summary>
/// Deletes the caller's own UI comment (DELETE /ui-feedback/{Id}).
/// </summary>
public abstract class DeleteUiFeedbackEndpointBase : Endpoint<UiFeedbackIdRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteUiFeedbackEndpointBase"/> class.
    /// </summary>
    /// <param name="feedback">The UI comment service.</param>
    /// <param name="logger">The logger of the concrete endpoint.</param>
    protected DeleteUiFeedbackEndpointBase(IUiFeedbackService feedback, ILogger logger)
    {
        Feedback = feedback;
        EndpointLogger = logger;
    }

    /// <summary>Gets the UI comment service.</summary>
    protected IUiFeedbackService Feedback { get; }

    /// <summary>Gets the logger of the concrete endpoint.</summary>
    protected ILogger EndpointLogger { get; }

    /// <inheritdoc/>
    public override void Configure()
    {
        Delete("/ui-feedback/{Id}");
        Policies("ui-feedback:write");
        Summary(s => s.Summary = "Delete a UI comment");
        ConfigureEndpoint();
    }

    /// <summary>
    /// Additional endpoint-specific configuration. Override for custom setup, such as tags.
    /// </summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(UiFeedbackIdRequest req, CancellationToken ct)
    {
        MessagingEndpointLog.TraceUiFeedbackEndpoint(EndpointLogger, "delete");

        var deleted = await Feedback.Delete(req.Id, ct).ConfigureAwait(false);
        if (!deleted.IsSuccess)
        {
            await Send.ResultAsync(UiFeedbackFailures.Of(deleted, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }
}
