using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Messaging.Endpoints.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Messaging.Endpoints;

/// <summary>
/// Changes the note of the caller's own UI comment (PATCH /ui-feedback/{Id}).
/// </summary>
public abstract class EditUiFeedbackEndpointBase : Endpoint<EditUiFeedbackEndpointRequest, UiFeedbackResponse>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EditUiFeedbackEndpointBase"/> class.
    /// </summary>
    /// <param name="feedback">The UI comment service.</param>
    /// <param name="logger">The logger of the concrete endpoint.</param>
    protected EditUiFeedbackEndpointBase(IUiFeedbackService feedback, ILogger logger)
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
        Patch("/ui-feedback/{Id}");
        Policies("ui-feedback:write");
        Summary(s => s.Summary = "Change the note of a UI comment");
        ConfigureEndpoint();
    }

    /// <summary>
    /// Additional endpoint-specific configuration. Override for custom setup, such as tags.
    /// </summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(EditUiFeedbackEndpointRequest req, CancellationToken ct)
    {
        MessagingEndpointLog.TraceUiFeedbackEndpoint(EndpointLogger, "edit");

        var edited = await Feedback.Edit(req.Id, req.Body, ct).ConfigureAwait(false);
        if (!edited.IsSuccess || edited.Value is not { } comment)
        {
            await Send.ResultAsync(UiFeedbackFailures.Of(edited, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(UiFeedbackResponse.Of(comment), ct).ConfigureAwait(false);
    }
}
