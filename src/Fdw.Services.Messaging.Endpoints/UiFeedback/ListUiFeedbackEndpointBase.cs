using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Messaging.Endpoints.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Messaging.Endpoints;

/// <summary>
/// Lists the UI comments the caller reaches (GET /ui-feedback), answering <c>{ items: [...] }</c> without pictures.
/// </summary>
public abstract class ListUiFeedbackEndpointBase : Endpoint<ListUiFeedbackEndpointRequest, UiFeedbackListResponse>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListUiFeedbackEndpointBase"/> class.
    /// </summary>
    /// <param name="feedback">The UI comment service.</param>
    /// <param name="logger">The logger of the concrete endpoint.</param>
    protected ListUiFeedbackEndpointBase(IUiFeedbackService feedback, ILogger logger)
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
        Get("/ui-feedback");
        Policies("ui-feedback:read|ui-feedback:write");
        Summary(s => s.Summary = "List UI comments");
        ConfigureEndpoint();
    }

    /// <summary>
    /// Additional endpoint-specific configuration. Override for custom setup, such as tags.
    /// </summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(ListUiFeedbackEndpointRequest req, CancellationToken ct)
    {
        MessagingEndpointLog.TraceUiFeedbackEndpoint(EndpointLogger, "list");

        if (req.Sort is not null && !string.Equals(req.Sort, "urgency", StringComparison.OrdinalIgnoreCase))
        {
            await Send.ResultAsync(UiFeedbackFailures.Of(GenericResult.Failure(MessagingEndpointLog.UiFeedbackSortUnknown(EndpointLogger, req.Sort)), HttpContext)).ConfigureAwait(false);
            return;
        }

        var listed = await Feedback.List(
            new UiFeedbackQuery
            {
                State = req.State,
                Route = req.Route,
                Urgency = req.Urgency,
                Tag = req.Tag,
                SortByUrgency = req.Sort is not null,
                Skip = req.Skip,
                Take = req.Take
            },
            ct).ConfigureAwait(false);

        if (!listed.IsSuccess || listed.Value is not { } comments)
        {
            await Send.ResultAsync(UiFeedbackFailures.Of(listed, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(new UiFeedbackListResponse { Items = comments.Select(UiFeedbackResponse.Of).ToList() }, ct).ConfigureAwait(false);
    }
}
