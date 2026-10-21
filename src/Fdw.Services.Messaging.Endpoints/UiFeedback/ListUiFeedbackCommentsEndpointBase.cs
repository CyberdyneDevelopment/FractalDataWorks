using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Messaging.Endpoints.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Messaging.Endpoints;

/// <summary>
/// Lists the comments on a UI note, oldest first (GET /ui-feedback/{Id}/comments) (FDW-2248).
/// </summary>
public abstract class ListUiFeedbackCommentsEndpointBase : Endpoint<ListUiFeedbackCommentsEndpointRequest, UiFeedbackCommentListResponse>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListUiFeedbackCommentsEndpointBase"/> class.
    /// </summary>
    /// <param name="feedback">The UI comment service.</param>
    /// <param name="logger">The logger of the concrete endpoint.</param>
    protected ListUiFeedbackCommentsEndpointBase(IUiFeedbackService feedback, ILogger logger)
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
        Get("/ui-feedback/{Id}/comments");
        Policies("ui-feedback:read|ui-feedback:write");
        Summary(s => s.Summary = "List the comments on a UI note");
        ConfigureEndpoint();
    }

    /// <summary>
    /// Additional endpoint-specific configuration. Override for custom setup, such as tags.
    /// </summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(ListUiFeedbackCommentsEndpointRequest req, CancellationToken ct)
    {
        MessagingEndpointLog.TraceUiFeedbackEndpoint(EndpointLogger, "list comments");

        var listed = await Feedback.ListComments(req.Id, req.Skip, req.Take, ct).ConfigureAwait(false);
        if (!listed.IsSuccess || listed.Value is not { } comments)
        {
            await Send.ResultAsync(UiFeedbackFailures.Of(listed, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(new UiFeedbackCommentListResponse { Items = comments.Select(UiFeedbackCommentResponse.Of).ToList() }, ct).ConfigureAwait(false);
    }
}
