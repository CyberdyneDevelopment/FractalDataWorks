using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Messaging.Endpoints.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Messaging.Endpoints;

/// <summary>
/// Reads the picture of the screen a UI comment was left on (GET /ui-feedback/{Id}/screenshot), answered as the picture itself.
/// </summary>
public abstract class GetUiFeedbackScreenshotEndpointBase : Endpoint<UiFeedbackIdRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetUiFeedbackScreenshotEndpointBase"/> class.
    /// </summary>
    /// <param name="feedback">The UI comment service.</param>
    /// <param name="logger">The logger of the concrete endpoint.</param>
    protected GetUiFeedbackScreenshotEndpointBase(IUiFeedbackService feedback, ILogger logger)
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
        Get("/ui-feedback/{Id}/screenshot");
        Policies("ui-feedback:read|ui-feedback:write");
        Summary(s => s.Summary = "Read the picture of a UI comment");
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
        MessagingEndpointLog.TraceUiFeedbackEndpoint(EndpointLogger, "get-screenshot");

        var read = await Feedback.GetPicture(req.Id, ct).ConfigureAwait(false);
        if (!read.IsSuccess || read.Value is not { } picture)
        {
            await Send.ResultAsync(MessagingFailures.Of(read, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.ResultAsync(TypedResults.Bytes(picture.Content, picture.ContentType)).ConfigureAwait(false);
    }
}
