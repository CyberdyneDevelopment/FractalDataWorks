using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.SessionState;
using Fdw.Web.Endpoints.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Claims.Abstractions;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Fdw.Results;

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
        if (CallerClaims.UserId(User) is not { } userId)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(SessionStateEndpointLog.ClearSessionStateNotSignedIn(_logger)), HttpContext)).ConfigureAwait(false);
            return;
        }

        SessionStateEndpointLog.ClearingAll(_logger, userId);

        var result = await _sessionState.ClearAll(userId, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            SessionStateEndpointLog.OperationFailed(_logger, "ClearAll");
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(result, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }
}
