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
using Fdw.Services.Authentication.Clients.Models;

namespace Fdw.Services.Authentication.Endpoints;

/// <summary>
/// Abstract base class for creating an agent key (POST /agent-keys).
/// The raw key value is returned exactly once in the response.
/// </summary>
public abstract class CreateAgentKeyEndpointBase
    : Endpoint<CreateAgentKeyRequest, CreateAgentKeyResponse>
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateAgentKeyEndpointBase"/> class.
    /// </summary>
    protected CreateAgentKeyEndpointBase(ILoggerFactory loggerFactory)
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
            s.Summary = "Create agent key";
            s.Description = "Creates a new agent key for the current user. The raw key value is returned only once.";
        });
        Tags("Authentication");
        ConfigureEndpoint();
    }

    /// <summary>Additional endpoint-specific configuration.</summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(CreateAgentKeyRequest req, CancellationToken ct)
    {
        if (!EndpointUserResolver.TryResolveUserId(User, out var userId))
        {
            AuthenticationEndpointLog.UserIdentityNotFound(_logger);
            await Send.UnauthorizedAsync(ct).ConfigureAwait(false);
            return;
        }

        AuthenticationEndpointLog.CreatingAgentKey(_logger, req.Label);

        try
        {
            var result = await CreateKey(userId, req.Label, req.ExpiresAt, ct).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                AddError("Failed to create agent key.");
                await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
                return;
            }

            var created = result.Value!;
            AuthenticationEndpointLog.AgentKeyCreated(_logger, created.Label);

            await Send.ResponseAsync(new CreateAgentKeyResponse
            {
                KeyId = created.KeyId,
                RawKey = created.RawKey,
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
            AuthenticationEndpointLog.AuthenticationException(_logger, ex, "create-agent-key");
            await Send.StatusCodeAsync(500, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Creates the agent key for the resolved user. Implementers delegate to
    /// <see cref="IAgentKeyService"/> (resolving the owning user's display name as needed).
    /// </summary>
    protected abstract Task<IGenericResult<AgentKeyCreatedResult>> CreateKey(
        Guid userId, string label, DateTime? expiresAt, CancellationToken ct);
}
