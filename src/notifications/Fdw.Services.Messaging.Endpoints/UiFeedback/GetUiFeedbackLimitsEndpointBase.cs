using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Messaging.Endpoints.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Messaging.Endpoints;

/// <summary>
/// Reads the limits a UI comment is held to (GET /ui-feedback/limits), as the host configured them (FDW-7058): the console checks a note, its components
/// and its picture against these, so it carries no number of its own.
/// </summary>
public abstract class GetUiFeedbackLimitsEndpointBase : EndpointWithoutRequest<UiFeedbackLimitsResponse>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetUiFeedbackLimitsEndpointBase"/> class.
    /// </summary>
    /// <param name="feedback">The UI comment service.</param>
    /// <param name="logger">The logger of the concrete endpoint.</param>
    protected GetUiFeedbackLimitsEndpointBase(IUiFeedbackService feedback, ILogger logger)
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
        Get("/ui-feedback/limits");
        Policies("authenticated");
        Summary(s =>
        {
            s.Summary = "Read the UI comment limits";
            s.Description = "Answers the most characters a note holds, the most a picture of the screen weighs in bytes, the most components a comment names and how long each may be, and the most comments a list returns, as this host configured them.";
        });
        ConfigureEndpoint();
    }

    /// <summary>
    /// Additional endpoint-specific configuration. Override for custom setup, such as tags.
    /// </summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(CancellationToken ct)
    {
        MessagingEndpointLog.TraceUiFeedbackEndpoint(EndpointLogger, "limits");
        await Send.OkAsync(UiFeedbackLimitsResponse.Of(Feedback.Limits), ct).ConfigureAwait(false);
    }
}
