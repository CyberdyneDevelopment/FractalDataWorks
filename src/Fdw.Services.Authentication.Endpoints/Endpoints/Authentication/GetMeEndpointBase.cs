using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Authentication.Endpoints.Models;
using Fdw.Web.RestEndpoints.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using Fdw.Services.Authentication.Clients.Models;

namespace Fdw.Services.Authentication.Endpoints;

/// <summary>
/// Abstract base class for retrieving the current authenticated user's information (GET /users/me).
/// Extracts user identity from the authentication context.
/// This is an authenticated endpoint (requires valid credentials).
/// </summary>
/// <typeparam name="TResponse">The response type. Must inherit from <see cref="GetMePayload"/>.</typeparam>
public abstract class GetMeEndpointBase<TResponse> : EndpointWithoutRequest<TResponse>
    where TResponse : GetMePayload, new()
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetMeEndpointBase{TResponse}"/> class.
    /// </summary>
    protected GetMeEndpointBase(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger(GetType());
    }

    /// <summary>
    /// Gets the logger.
    /// </summary>
    protected ILogger EndpointLogger => _logger;

    /// <summary>
    /// Gets the route for this endpoint. Defaults to "/users/me".
    /// </summary>
    protected virtual string Route => "/users/me";

    /// <summary>
    /// Gets the rate limit policy name. Defaults to <see cref="RateLimitPolicyNames.Authenticated"/>.
    /// </summary>
    protected virtual string? RateLimitPolicy => RateLimitPolicyNames.Authenticated;

    /// <summary>
    /// Gets the endpoint summary for OpenAPI documentation.
    /// </summary>
    protected virtual string EndpointSummary => "Get current user";

    /// <summary>
    /// Gets the endpoint description for OpenAPI documentation.
    /// </summary>
    protected virtual string EndpointDescription =>
        "Returns information about the currently authenticated user including their roles and permissions.";

    /// <summary>
    /// Gets the endpoint tag for OpenAPI grouping.
    /// </summary>
    protected virtual string? EndpointTag => "Authentication";

    /// <inheritdoc/>
    public override void Configure()
    {
        Get(Route);
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
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
        {
            AuthenticationEndpointLog.UserIdentityNotFound(_logger);
            await Send.UnauthorizedAsync(ct).ConfigureAwait(false);
            return;
        }

        AuthenticationEndpointLog.GettingUserInfo(_logger);

        try
        {
            var result = await GetUserInfo(username, ct).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                await Send.NotFoundAsync(ct).ConfigureAwait(false);
                return;
            }

            AuthenticationEndpointLog.UserInfoRetrieved(_logger, username);
            await Send.OkAsync(result.Value!, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            AuthenticationEndpointLog.AuthenticationException(_logger, ex, "get-user-info");
            await Send.StatusCodeAsync(500, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Retrieves the user information for the authenticated user.
    /// Implementers must override this to look up user details from the appropriate store.
    /// </summary>
    /// <param name="username">The authenticated username from the claims principal.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the user information response.</returns>
    protected abstract Task<IGenericResult<TResponse>> GetUserInfo(string username, CancellationToken ct);
}
