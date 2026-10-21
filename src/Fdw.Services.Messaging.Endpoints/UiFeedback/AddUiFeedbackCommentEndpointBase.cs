using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Messaging.Endpoints.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Messaging.Endpoints;

/// <summary>
/// Adds a comment to a UI note (POST /ui-feedback/{Id}/comments), answering 201 with the comment as kept (FDW-2248).
/// </summary>
public abstract class AddUiFeedbackCommentEndpointBase : Endpoint<AddUiFeedbackCommentEndpointRequest, UiFeedbackCommentResponse>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddUiFeedbackCommentEndpointBase"/> class.
    /// </summary>
    /// <param name="feedback">The UI comment service.</param>
    /// <param name="logger">The logger of the concrete endpoint.</param>
    protected AddUiFeedbackCommentEndpointBase(IUiFeedbackService feedback, ILogger logger)
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
        Post("/ui-feedback/{Id}/comments");
        Policies("ui-feedback:write");
        Summary(s => s.Summary = "Comment on a UI note");
        ConfigureEndpoint();
    }

    /// <summary>
    /// Additional endpoint-specific configuration. Override for custom setup, such as tags.
    /// </summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(AddUiFeedbackCommentEndpointRequest req, CancellationToken ct)
    {
        MessagingEndpointLog.TraceUiFeedbackEndpoint(EndpointLogger, "add a comment");

        var added = await Feedback.AddComment(req.Id, req.Body, ct).ConfigureAwait(false);
        if (!added.IsSuccess || added.Value is not { } comment)
        {
            await Send.ResultAsync(UiFeedbackFailures.Of(added, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.ResponseAsync(UiFeedbackCommentResponse.Of(comment), 201, ct).ConfigureAwait(false);
    }
}
