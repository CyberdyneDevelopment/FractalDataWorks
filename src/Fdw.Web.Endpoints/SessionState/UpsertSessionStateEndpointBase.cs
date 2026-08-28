using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.SessionState;
using Fdw.Web.Endpoints.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.Endpoints.SessionState;

/// <summary>
/// Base endpoint for upserting a session state value for the authenticated user.
/// Route: PUT /session-state/{Key}
/// </summary>
public abstract class UpsertSessionStateEndpointBase : Endpoint<UpsertSessionStateRequest>
{
    private readonly ISessionStateService _sessionState;
    private readonly ILogger<UpsertSessionStateEndpointBase> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpsertSessionStateEndpointBase"/> class.
    /// </summary>
    protected UpsertSessionStateEndpointBase(ISessionStateService sessionState, ILogger<UpsertSessionStateEndpointBase> logger)
    {
        _sessionState = sessionState;
        _logger = logger ?? NullLogger<UpsertSessionStateEndpointBase>.Instance;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Patch("/session-state/{Key}");
        Policies("authenticated");
        Summary(s => s.Summary = "Upsert session state value");
        ConfigureEndpoint();
    }

    /// <summary>Override to add Tags or other endpoint configuration.</summary>
    protected virtual void ConfigureEndpoint() { }

    /// <inheritdoc/>
    public override async Task HandleAsync(UpsertSessionStateRequest req, CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value ?? string.Empty;
        SessionStateEndpointLog.UpsertingState(_logger, userId, req.Key);

        var result = await _sessionState.SaveState(userId, req.Key, req.Value, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            SessionStateEndpointLog.OperationFailed(_logger, "Upsert");
            AddError("Failed to save session state");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }
}
