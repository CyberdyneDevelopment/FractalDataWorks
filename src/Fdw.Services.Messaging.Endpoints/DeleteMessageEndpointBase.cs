using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Messaging.Endpoints.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Messaging.Endpoints;

/// <summary>
/// Abstract base class for deleting a message (DELETE /messages/{Id}).
/// Why: Messaging service has no hard-delete primitive; "delete" is the Dismiss action of the message's alert (FDW-2429), the user
/// removing it from their inbox.
/// </summary>
public abstract class DeleteMessageEndpointBase : Endpoint<MessageIdRequest>
{
    private readonly IAlertActionService _alerts;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteMessageEndpointBase"/> class.
    /// </summary>
    protected DeleteMessageEndpointBase(
        IAlertActionService alerts,
        ILogger logger)
    {
        _alerts = alerts;
        _logger = logger;
    }

    /// <summary>Gets the alert action service.</summary>
    protected IAlertActionService Alerts => _alerts;

    /// <summary>Gets the logger.</summary>
    protected ILogger EndpointLogger => _logger;

    /// <inheritdoc/>
    public override void Configure()
    {
        Delete("/messages/{Id}");
        Policies("messages:read");
        Summary(s => s.Summary = "Delete a message");
        ConfigureEndpoint();
    }

    /// <summary>Override for endpoint-specific configuration.</summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(MessageIdRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _alerts.Apply(MessageAlert.Kind, req.Id, MessageAlert.DismissAction, null, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                await Send.ResultAsync(MessagingFailures.Of(result, HttpContext)).ConfigureAwait(false);
                return;
            }

            await Send.NoContentAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            MessagingEndpointLog.MessagingException(_logger, ex, "delete-message");
            await Send.StatusCodeAsync(500, ct).ConfigureAwait(false);
        }
    }
}
