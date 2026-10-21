using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Messaging.Endpoints.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Messaging.Endpoints;

/// <summary>
/// Agrees with a UI comment as the caller (POST /ui-feedback/{Id}/agree), answering the comment with its agree count and agreedByMe set. Agreeing again changes nothing;
/// the person who left the comment cannot agree with it (FDW-2247).
/// </summary>
public abstract class AgreeUiFeedbackEndpointBase : Endpoint<UiFeedbackIdRequest, UiFeedbackResponse>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgreeUiFeedbackEndpointBase"/> class.
    /// </summary>
    /// <param name="feedback">The UI comment service.</param>
    /// <param name="logger">The logger of the concrete endpoint.</param>
    protected AgreeUiFeedbackEndpointBase(IUiFeedbackService feedback, ILogger logger)
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
        Post("/ui-feedback/{Id}/agree");
        Policies("ui-feedback:write");
        Summary(s => s.Summary = "Agree with a UI comment");
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
        MessagingEndpointLog.TraceUiFeedbackEndpoint(EndpointLogger, "agree");

        var moved = await Feedback.Agree(req.Id, ct).ConfigureAwait(false);
        if (!moved.IsSuccess || moved.Value is not { } comment)
        {
            await Send.ResultAsync(UiFeedbackFailures.Of(moved, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(UiFeedbackResponse.Of(comment), ct).ConfigureAwait(false);
    }
}
