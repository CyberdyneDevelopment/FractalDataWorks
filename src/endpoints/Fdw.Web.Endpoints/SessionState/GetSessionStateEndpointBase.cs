using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.SessionState;
using Fdw.Web.Endpoints.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.Endpoints.SessionState;

/// <summary>
/// Base endpoint for getting a session state value by key for the authenticated user.
/// Route: GET /session-state/{Key}
/// </summary>
public abstract class GetSessionStateEndpointBase : Endpoint<SessionStateKeyRequest, SessionStateEntryResponse>
{
    private readonly ISessionStateService _sessionState;
    private readonly ILogger<GetSessionStateEndpointBase> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetSessionStateEndpointBase"/> class.
    /// </summary>
    protected GetSessionStateEndpointBase(ISessionStateService sessionState, ILogger<GetSessionStateEndpointBase> logger)
    {
        _sessionState = sessionState;
        _logger = logger ?? NullLogger<GetSessionStateEndpointBase>.Instance;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Get("/session-state/{Key}");
        Policies("authenticated");
        Summary(s => s.Summary = "Get session state value by key");
        ConfigureEndpoint();
    }

    /// <summary>Override to add Tags or other endpoint configuration.</summary>
    protected virtual void ConfigureEndpoint() { }

    /// <inheritdoc/>
    public override async Task HandleAsync(SessionStateKeyRequest req, CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value ?? string.Empty;
        SessionStateEndpointLog.GettingState(_logger, userId, req.Key);

        var result = await _sessionState.GetState<JsonElement>(userId, req.Key, ct).ConfigureAwait(false);

        if (!result.IsSuccess || result.Value.ValueKind == JsonValueKind.Undefined)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(new SessionStateEntryResponse
        {
            Key = req.Key,
            Value = result.Value
        }, ct).ConfigureAwait(false);
    }
}
