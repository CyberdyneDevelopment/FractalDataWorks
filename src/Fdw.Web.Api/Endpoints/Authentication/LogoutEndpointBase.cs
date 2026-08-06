using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Web.RestEndpoints.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Endpoints;

/// <summary>
/// Abstract base class for logout endpoints (POST /auth/logout).
/// Invalidates the current user's authentication tokens.
/// This is an authenticated endpoint (requires valid credentials).
/// </summary>
public abstract class LogoutEndpointBase : EndpointWithoutRequest
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogoutEndpointBase"/> class.
    /// </summary>
    protected LogoutEndpointBase(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger(GetType());
    }

    /// <summary>
    /// Gets the logger.
    /// </summary>
    protected ILogger EndpointLogger => _logger;

    /// <summary>
    /// Gets the route for this endpoint. Defaults to "/auth/logout".
    /// </summary>
    protected virtual string Route => "/auth/logout";

    /// <summary>
    /// Gets the rate limit policy name. Defaults to <see cref="RateLimitPolicyNames.Authenticated"/>.
    /// </summary>
    protected virtual string? RateLimitPolicy => RateLimitPolicyNames.Authenticated;

    /// <summary>
    /// Gets the endpoint summary for OpenAPI documentation.
    /// </summary>
    protected virtual string EndpointSummary => "Logout and invalidate tokens";

    /// <summary>
    /// Gets the endpoint description for OpenAPI documentation.
    /// </summary>
    protected virtual string EndpointDescription =>
        "Invalidates all refresh tokens for the current user.";

    /// <summary>
    /// Gets the endpoint tag for OpenAPI grouping.
    /// </summary>
    protected virtual string? EndpointTag => "Authentication";

    /// <inheritdoc/>
    public override void Configure()
    {
        Post(Route);
#if DEVELOP
        AllowAnonymous();
#endif

        if (!string.IsNullOrEmpty(RateLimitPolicy))
        {
            Options(x => x.RequireRateLimiting(RateLimitPolicy));
        }

        if (!string.IsNullOrEmpty(EndpointSummary) || !string.IsNullOrEmpty(EndpointDescription))
        {
            Summary(s =>
            {
                s.Summary = EndpointSummary;
                s.Description = EndpointDescription;
            });
        }

        if (!string.IsNullOrEmpty(EndpointTag))
        {
            Description(x => x.WithTags(EndpointTag));
        }

        ConfigureEndpoint();
    }

    /// <summary>
    /// Additional endpoint-specific configuration. Override for custom setup.
    /// </summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(CancellationToken ct)
    {
        var username = User.Identity?.Name ?? "Unknown";

        AuthenticationEndpointLog.LoggingOut(_logger, username);

        try
        {
            await PerformLogout(username, ct).ConfigureAwait(false);

            AuthenticationEndpointLog.LogoutCompleted(_logger, username);
            await Send.NoContentAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            AuthenticationEndpointLog.AuthenticationException(_logger, ex, "logout");
            await Send.StatusCodeAsync(500, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Performs the logout operation. Implementers must override this to handle
    /// token revocation, session cleanup, etc.
    /// </summary>
    /// <param name="username">The username of the user being logged out.</param>
    /// <param name="ct">Cancellation token.</param>
    protected abstract Task PerformLogout(string username, CancellationToken ct);
}
