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

namespace Fdw.Services.Authentication.Endpoints;

/// <summary>
/// Abstract base class for listing the current user's agent keys (GET /agent-keys).
/// Returns summaries only — never a raw key value.
/// </summary>
public abstract class ListAgentKeysEndpointBase
    : EndpointWithoutRequest<IReadOnlyList<AgentKeySummary>>
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListAgentKeysEndpointBase"/> class.
    /// </summary>
    protected ListAgentKeysEndpointBase(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger(GetType());
    }

    /// <summary>Gets the logger.</summary>
    protected ILogger EndpointLogger => _logger;

    /// <summary>Gets the route. Defaults to "/agent-keys".</summary>
    protected virtual string Route => "/agent-keys";

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
            s.Summary = "List agent keys";
            s.Description = "Returns all active agent keys for the current user (summaries only).";
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

        AuthenticationEndpointLog.ListingAgentKeys(_logger);

        try
        {
            var result = await ListKeys(userId, ct).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                await Send.StatusCodeAsync(500, ct).ConfigureAwait(false);
                return;
            }

            IReadOnlyList<AgentKeySummary> response = result.Value!;

            await Send.OkAsync(response, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            AuthenticationEndpointLog.AuthenticationException(_logger, ex, "list-agent-keys");
            await Send.StatusCodeAsync(500, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Lists the agent keys for the resolved user. Implementers delegate to <see cref="IAgentKeyService"/>.
    /// </summary>
    protected abstract Task<IGenericResult<IReadOnlyList<AgentKeySummary>>> ListKeys(
        Guid userId, CancellationToken ct);
}
