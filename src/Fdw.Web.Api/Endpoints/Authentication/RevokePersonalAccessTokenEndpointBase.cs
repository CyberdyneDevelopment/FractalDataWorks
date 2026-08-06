using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Methods;
using Fdw.Services.Authentication.Endpoints.Models;
using Fdw.Web.RestEndpoints.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Endpoints;

/// <summary>
/// Abstract base class for revoking a personal access token (DELETE /users/me/tokens/{tokenId}).
/// </summary>
public abstract class RevokePersonalAccessTokenEndpointBase
    : Endpoint<RevokePersonalAccessTokenRequest>
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RevokePersonalAccessTokenEndpointBase"/> class.
    /// </summary>
    protected RevokePersonalAccessTokenEndpointBase(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger(GetType());
    }

    /// <summary>Gets the logger.</summary>
    protected ILogger EndpointLogger => _logger;

    /// <summary>Gets the route. Defaults to "/users/me/tokens/{tokenId}".</summary>
    protected virtual string Route => "/users/me/tokens/{tokenId}";

    /// <summary>Gets the rate limit policy name.</summary>
    protected virtual string? RateLimitPolicy => RateLimitPolicyNames.Authenticated;

    /// <inheritdoc/>
    public override void Configure()
    {
        Delete(Route);
#if DEVELOP
        AllowAnonymous();
#endif
        if (!string.IsNullOrEmpty(RateLimitPolicy))
        {
            Options(x => x.RequireRateLimiting(RateLimitPolicy));
        }

        Summary(s =>
        {
            s.Summary = "Revoke personal access token";
            s.Description = "Revokes a personal access token owned by the current user.";
        });
        Tags("Authentication");
        ConfigureEndpoint();
    }

    /// <summary>Additional endpoint-specific configuration.</summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(RevokePersonalAccessTokenRequest req, CancellationToken ct)
    {
        if (!EndpointUserResolver.TryResolveUserId(User, out var userId))
        {
            AuthenticationEndpointLog.UserIdentityNotFound(_logger);
            await Send.UnauthorizedAsync(ct).ConfigureAwait(false);
            return;
        }

        AuthenticationEndpointLog.RevokingPersonalAccessToken(_logger, req.TokenId.ToString(), userId.ToString());

        try
        {
            var result = await RevokeToken(userId, req.TokenId, ct).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                await Send.NotFoundAsync(ct).ConfigureAwait(false);
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
            AuthenticationEndpointLog.AuthenticationException(_logger, ex, "revoke-personal-access-token");
            await Send.StatusCodeAsync(500, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Revokes the personal access token for the resolved user. Implementers delegate to
    /// <see cref="IPersonalAccessTokenService"/>.
    /// </summary>
    protected abstract Task<IGenericResult> RevokeToken(Guid userId, Guid tokenId, CancellationToken ct);
}
