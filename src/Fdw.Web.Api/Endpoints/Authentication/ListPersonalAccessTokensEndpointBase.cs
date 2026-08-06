using System;
using System.Collections.Generic;
using System.Linq;
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
using PersonalAccessTokenSummary = Fdw.Services.Authentication.Endpoints.Models.PersonalAccessTokenSummary;
using ServiceTokenSummary = Fdw.Services.Authentication.Abstractions.Methods.PersonalAccessTokenSummary;

namespace Fdw.Services.Authentication.Endpoints;

/// <summary>
/// Abstract base class for listing the current user's personal access tokens (GET /users/me/tokens).
/// Returns summaries only — never a raw token value.
/// </summary>
public abstract class ListPersonalAccessTokensEndpointBase
    : EndpointWithoutRequest<IReadOnlyList<PersonalAccessTokenSummary>>
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListPersonalAccessTokensEndpointBase"/> class.
    /// </summary>
    protected ListPersonalAccessTokensEndpointBase(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger(GetType());
    }

    /// <summary>Gets the logger.</summary>
    protected ILogger EndpointLogger => _logger;

    /// <summary>Gets the route. Defaults to "/users/me/tokens".</summary>
    protected virtual string Route => "/users/me/tokens";

    /// <summary>Gets the rate limit policy name.</summary>
    protected virtual string? RateLimitPolicy => RateLimitPolicyNames.Authenticated;

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

        Summary(s =>
        {
            s.Summary = "List personal access tokens";
            s.Description = "Returns all active personal access tokens for the current user (summaries only).";
        });
        Tags("Authentication");
        ConfigureEndpoint();
    }

    /// <summary>Additional endpoint-specific configuration.</summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!EndpointUserResolver.TryResolveUserId(User, out var userId))
        {
            AuthenticationEndpointLog.UserIdentityNotFound(_logger);
            await Send.UnauthorizedAsync(ct).ConfigureAwait(false);
            return;
        }

        AuthenticationEndpointLog.ListingPersonalAccessTokens(_logger, userId.ToString());

        try
        {
            var result = await ListTokens(userId, ct).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                await Send.StatusCodeAsync(500, ct).ConfigureAwait(false);
                return;
            }

            IReadOnlyList<PersonalAccessTokenSummary> response = result.Value!
                .Select(s => new PersonalAccessTokenSummary
                {
                    TokenId = s.TokenId,
                    Prefix = s.Prefix,
                    Label = s.Label,
                    CreatedAt = s.CreatedAt,
                    ExpiresAt = s.ExpiresAt,
                    LastUsedAt = s.LastUsedAt
                })
                .ToList();

            await Send.OkAsync(response, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            AuthenticationEndpointLog.AuthenticationException(_logger, ex, "list-personal-access-tokens");
            await Send.StatusCodeAsync(500, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Lists the personal access tokens for the resolved user. Implementers delegate to
    /// <see cref="IPersonalAccessTokenService"/>.
    /// </summary>
    protected abstract Task<IGenericResult<IReadOnlyList<ServiceTokenSummary>>> ListTokens(
        Guid userId, CancellationToken ct);
}
