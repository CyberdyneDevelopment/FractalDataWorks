using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.SessionState;
using Fdw.Web.Endpoints.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.Endpoints.SessionState;

/// <summary>
/// Base endpoint for listing all session state keys for the authenticated user.
/// Route: GET /session-state
/// </summary>
public abstract class ListSessionStateKeysEndpointBase : EndpointWithoutRequest<SessionStateKeysResponse>
{
    private readonly ISessionStateService _sessionState;
    private readonly ILogger<ListSessionStateKeysEndpointBase> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListSessionStateKeysEndpointBase"/> class.
    /// </summary>
    protected ListSessionStateKeysEndpointBase(ISessionStateService sessionState, ILogger<ListSessionStateKeysEndpointBase> logger)
    {
        _sessionState = sessionState;
        _logger = logger ?? NullLogger<ListSessionStateKeysEndpointBase>.Instance;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Get("/session-state");
        Policies("authenticated");
        Summary(s => s.Summary = "List all session state keys");
        ConfigureEndpoint();
    }

    /// <summary>Override to add Tags or other endpoint configuration.</summary>
    protected virtual void ConfigureEndpoint() { }

    /// <inheritdoc/>
    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value ?? string.Empty;
        SessionStateEndpointLog.ListingKeys(_logger, userId);

        var result = await _sessionState.GetAllKeys(userId, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            SessionStateEndpointLog.OperationFailed(_logger, "ListKeys");
            AddError("Failed to list session state keys");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(new SessionStateKeysResponse { Keys = result.Value! }, ct).ConfigureAwait(false);
    }
}
