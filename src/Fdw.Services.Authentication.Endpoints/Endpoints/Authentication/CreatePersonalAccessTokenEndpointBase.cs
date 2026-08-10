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
/// Abstract base class for creating a personal access token (POST /users/me/tokens).
/// The raw token value is returned exactly once in the response.
/// </summary>
public abstract class CreatePersonalAccessTokenEndpointBase
    : Endpoint<CreatePersonalAccessTokenRequest, CreatePersonalAccessTokenResponse>
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreatePersonalAccessTokenEndpointBase"/> class.
    /// </summary>
    protected CreatePersonalAccessTokenEndpointBase(ILoggerFactory loggerFactory)
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
        Post(Route);
#if DEVELOP
        AllowAnonymous();
#endif
        if (!string.IsNullOrEmpty(RateLimitPolicy))
        {
            Options(x => x.RequireRateLimiting(RateLimitPolicy));
        }

        Summary(s =>
        {
            s.Summary = "Create personal access token";
            s.Description = "Creates a new personal access token for the current user. The raw token value is returned only once.";
        });
        Tags("Authentication");
        ConfigureEndpoint();
    }

    /// <summary>Additional endpoint-specific configuration.</summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(CreatePersonalAccessTokenRequest req, CancellationToken ct)
    {
        if (!EndpointUserResolver.TryResolveUserId(User, out var userId))
        {
            AuthenticationEndpointLog.UserIdentityNotFound(_logger);
            await Send.UnauthorizedAsync(ct).ConfigureAwait(false);
            return;
        }

        AuthenticationEndpointLog.CreatingPersonalAccessToken(_logger, userId.ToString());

        try
        {
            var result = await CreateToken(userId, req.Label, req.ExpiresAt, ct).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                AddError("Failed to create personal access token.");
                await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
                return;
            }

            var created = result.Value!;
            AuthenticationEndpointLog.PersonalAccessTokenCreated(_logger, userId.ToString());

            await Send.ResponseAsync(new CreatePersonalAccessTokenResponse
            {
                TokenId = created.TokenId,
                RawToken = created.RawToken,
                Prefix = created.Prefix,
                Label = created.Label,
                ExpiresAt = created.ExpiresAt
            }, 201, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            AuthenticationEndpointLog.AuthenticationException(_logger, ex, "create-personal-access-token");
            await Send.StatusCodeAsync(500, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Creates the personal access token for the resolved user. Implementers delegate to
    /// <see cref="IPersonalAccessTokenService"/>.
    /// </summary>
    protected abstract Task<IGenericResult<PersonalAccessTokenCreatedResult>> CreateToken(
        Guid userId, string label, DateTime? expiresAt, CancellationToken ct);
}
