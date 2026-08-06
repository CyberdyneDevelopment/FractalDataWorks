using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.SessionState;
using Fdw.Web.Endpoints.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.Endpoints.SessionState;

/// <summary>
/// Base endpoint for clearing all session state entries for the authenticated user.
/// Route: DELETE /session-state
/// </summary>
public abstract class ClearSessionStateEndpointBase : EndpointWithoutRequest
{
    private readonly ISessionStateService _sessionState;
    private readonly ILogger<ClearSessionStateEndpointBase> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClearSessionStateEndpointBase"/> class.
    /// </summary>
    protected ClearSessionStateEndpointBase(ISessionStateService sessionState, ILogger<ClearSessionStateEndpointBase> logger)
    {
        _sessionState = sessionState;
        _logger = logger ?? NullLogger<ClearSessionStateEndpointBase>.Instance;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Delete("/session-state");
        Policies("authenticated");
        Summary(s => s.Summary = "Clear all session state for user");
        ConfigureEndpoint();
    }

    /// <summary>Override to add Tags or other endpoint configuration.</summary>
    protected virtual void ConfigureEndpoint() { }

    /// <inheritdoc/>
    public override async Task HandleAsync(CancellationToken ct)
    {
        // Why: JWT sub claim contains the user's durable GUID. Identity.Name is the username
        // which can't query a UNIQUEIDENTIFIER column.
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value ?? string.Empty;
        SessionStateEndpointLog.ClearingAll(_logger, userId);

        var result = await _sessionState.ClearAll(userId, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            SessionStateEndpointLog.OperationFailed(_logger, "ClearAll");
            AddError("Failed to clear session state");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }
}
