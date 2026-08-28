using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.SessionState;
using Fdw.Web.Endpoints.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.Endpoints.SessionState;

/// <summary>
/// Base endpoint for deleting a single session state entry for the authenticated user.
/// Route: DELETE /session-state/{Key}
/// </summary>
public abstract class DeleteSessionStateEndpointBase : Endpoint<SessionStateKeyRequest>
{
    private readonly ISessionStateService _sessionState;
    private readonly ILogger<DeleteSessionStateEndpointBase> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteSessionStateEndpointBase"/> class.
    /// </summary>
    protected DeleteSessionStateEndpointBase(ISessionStateService sessionState, ILogger<DeleteSessionStateEndpointBase> logger)
    {
        _sessionState = sessionState;
        _logger = logger ?? NullLogger<DeleteSessionStateEndpointBase>.Instance;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Delete("/session-state/{Key}");
        Policies("authenticated");
        Summary(s => s.Summary = "Delete a session state entry");
        ConfigureEndpoint();
    }

    /// <summary>Override to add Tags or other endpoint configuration.</summary>
    protected virtual void ConfigureEndpoint() { }

    /// <inheritdoc/>
    public override async Task HandleAsync(SessionStateKeyRequest req, CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value ?? string.Empty;
        SessionStateEndpointLog.DeletingState(_logger, userId, req.Key);

        var result = await _sessionState.DeleteState(userId, req.Key, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            SessionStateEndpointLog.OperationFailed(_logger, "Delete");
            AddError("Failed to delete session state");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }
}
