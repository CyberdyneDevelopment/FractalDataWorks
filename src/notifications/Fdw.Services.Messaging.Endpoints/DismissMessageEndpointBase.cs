using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Messaging.Endpoints.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Messaging.Endpoints;

/// <summary>
/// Abstract base class for dismissing a message (POST /messages/{Id}/dismiss). It is the Dismiss action of the message's alert (FDW-2429), the same as
/// POST /alerts/Message/{Id}/actions/Dismiss; a message that is not the caller's is not found.
/// </summary>
public abstract class DismissMessageEndpointBase : Endpoint<MessageIdRequest>
{
    private readonly IAlertActionService _alerts;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DismissMessageEndpointBase"/> class.
    /// </summary>
    /// <param name="alerts">The alert action service, which records the dismissal.</param>
    /// <param name="logger">The logger.</param>
    protected DismissMessageEndpointBase(
        IAlertActionService alerts,
        ILogger logger)
    {
        _alerts = alerts;
        _logger = logger;
    }

    /// <summary>
    /// Gets the alert action service.
    /// </summary>
    protected IAlertActionService Alerts => _alerts;

    /// <summary>
    /// Gets the logger.
    /// </summary>
    protected ILogger EndpointLogger => _logger;

    /// <inheritdoc/>
    public override void Configure()
    {
        Post("/messages/{Id}/dismiss");
        Policies("messages:read");
        Summary(s => s.Summary = "Dismiss a message");
        ConfigureEndpoint();
    }

    /// <summary>
    /// Additional endpoint-specific configuration. Override for custom setup.
    /// </summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(MessageIdRequest req, CancellationToken ct)
    {
        MessagingEndpointLog.DismissingMessage(_logger, req.Id.ToString());

        try
        {
            var result = await _alerts.Apply(MessageAlert.Kind, req.Id, MessageAlert.DismissAction, null, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                await Send.ResultAsync(MessagingFailures.Of(result, HttpContext)).ConfigureAwait(false);
                return;
            }

            MessagingEndpointLog.MessageDismissed(_logger, req.Id.ToString());
            await Send.NoContentAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            MessagingEndpointLog.MessagingException(_logger, ex, "dismiss-message");
            await Send.StatusCodeAsync(500, ct).ConfigureAwait(false);
        }
    }
}
