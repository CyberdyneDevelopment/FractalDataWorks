using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Messaging.Endpoints.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Messaging.Endpoints;

/// <summary>
/// Leaves a UI comment (POST /ui-feedback), answering 201 with the comment as saved.
/// </summary>
public abstract class CreateUiFeedbackEndpointBase : Endpoint<CreateUiFeedbackEndpointRequest, UiFeedbackResponse>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateUiFeedbackEndpointBase"/> class.
    /// </summary>
    /// <param name="feedback">The UI comment service.</param>
    /// <param name="logger">The logger of the concrete endpoint.</param>
    protected CreateUiFeedbackEndpointBase(IUiFeedbackService feedback, ILogger logger)
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
        Post("/ui-feedback");
        Policies("ui-feedback:write");
        Summary(s => s.Summary = "Leave a UI comment");
        ConfigureEndpoint();
    }

    /// <summary>
    /// Additional endpoint-specific configuration. Override for custom setup, such as tags.
    /// </summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(CreateUiFeedbackEndpointRequest req, CancellationToken ct)
    {
        MessagingEndpointLog.TraceUiFeedbackEndpoint(EndpointLogger, "create");

        var saved = await Feedback.Create(
            new CreateUiFeedbackRequest
            {
                Screen = req.Screen,
                Route = req.Route,
                Area = req.Area,
                Viewport = req.Viewport,
                Theme = req.Theme,
                AnchorJson = req.Anchor is { ValueKind: not (JsonValueKind.Null or JsonValueKind.Undefined) } anchor ? anchor.GetRawText() : null,
                Screenshot = req.Screenshot,
                Urgency = req.Urgency,
                Tags = req.Tags,
                Body = req.Body
            },
            ct).ConfigureAwait(false);

        if (!saved.IsSuccess || saved.Value is not { } comment)
        {
            await Send.ResultAsync(UiFeedbackFailures.Of(saved, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.ResponseAsync(UiFeedbackResponse.Of(comment), 201, ct).ConfigureAwait(false);
    }
}
