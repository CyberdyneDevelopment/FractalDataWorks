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
/// Abstract base class for deleting an agent key (DELETE /agent-keys/{keyId}).
/// </summary>
public abstract class DeleteAgentKeyEndpointBase
    : Endpoint<DeleteAgentKeyRequest>
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteAgentKeyEndpointBase"/> class.
    /// </summary>
    protected DeleteAgentKeyEndpointBase(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger(GetType());
    }

    /// <summary>Gets the logger.</summary>
    protected ILogger EndpointLogger => _logger;

    /// <summary>Gets the route. Defaults to "/agent-keys/{keyId}".</summary>
    protected virtual string Route => "/agent-keys/{keyId}";

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
            s.Summary = "Delete agent key";
            s.Description = "Deletes (deactivates) an agent key owned by the current user.";
        });
        Tags("Authentication");
        ConfigureEndpoint();
    }

    /// <summary>Additional endpoint-specific configuration.</summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(DeleteAgentKeyRequest req, CancellationToken ct)
    {
        if (!EndpointUserResolver.TryResolveUserId(User, out var userId))
        {
            AuthenticationEndpointLog.UserIdentityNotFound(_logger);
            await Send.UnauthorizedAsync(ct).ConfigureAwait(false);
            return;
        }

        AuthenticationEndpointLog.DeletingAgentKey(_logger, req.KeyId.ToString());

        try
        {
            var result = await DeleteKey(userId, req.KeyId, ct).ConfigureAwait(false);
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
            AuthenticationEndpointLog.AuthenticationException(_logger, ex, "delete-agent-key");
            await Send.StatusCodeAsync(500, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Deletes the agent key for the resolved user. Implementers delegate to <see cref="IAgentKeyService"/>.
    /// </summary>
    protected abstract Task<IGenericResult> DeleteKey(Guid userId, Guid keyId, CancellationToken ct);
}
