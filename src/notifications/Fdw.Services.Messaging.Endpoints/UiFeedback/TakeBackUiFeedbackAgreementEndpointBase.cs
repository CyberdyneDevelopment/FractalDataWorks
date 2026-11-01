using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Messaging.Endpoints.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Messaging.Endpoints;

/// <summary>
/// Takes back the caller's agreement with a UI comment (DELETE /ui-feedback/{Id}/agree), answering the comment with its agree count and agreedByMe cleared. Taking back an
/// agreement that was not given changes nothing (FDW-2247).
/// </summary>
public abstract class TakeBackUiFeedbackAgreementEndpointBase : Endpoint<UiFeedbackIdRequest, UiFeedbackResponse>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TakeBackUiFeedbackAgreementEndpointBase"/> class.
    /// </summary>
    /// <param name="feedback">The UI comment service.</param>
    /// <param name="logger">The logger of the concrete endpoint.</param>
    protected TakeBackUiFeedbackAgreementEndpointBase(IUiFeedbackService feedback, ILogger logger)
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
        Delete("/ui-feedback/{Id}/agree");
        Policies("ui-feedback:write");
        Summary(s => s.Summary = "Take back the agreement with a UI comment");
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
        MessagingEndpointLog.TraceUiFeedbackEndpoint(EndpointLogger, "take back the agreement");

        var moved = await Feedback.TakeBackAgreement(req.Id, ct).ConfigureAwait(false);
        if (!moved.IsSuccess || moved.Value is not { } comment)
        {
            await Send.ResultAsync(MessagingFailures.Of(moved, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(UiFeedbackResponse.Of(comment), ct).ConfigureAwait(false);
    }
}
