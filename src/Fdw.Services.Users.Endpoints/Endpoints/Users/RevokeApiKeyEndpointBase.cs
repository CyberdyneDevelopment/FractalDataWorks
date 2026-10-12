using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authentication.Abstractions.Methods;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Users.Clients.Models;
using Fdw.Services.Users.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Generic base endpoint revoking one of an account's API keys.
/// </summary>
/// <remarks>
/// The kind is part of the request because the two kinds live in different stores; revoking asks
/// the store that holds the key rather than guessing and reporting a miss as a success.
/// </remarks>
public abstract class RevokeApiKeyEndpointBase : Endpoint<RevokeApiKeyRequest>
{
    private readonly UserConfigurationProvider _userProvider;
    private readonly IPersonalAccessTokenService _tokenService;
    private readonly IAgentKeyService _agentKeyService;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="RevokeApiKeyEndpointBase"/> class.</summary>
    protected RevokeApiKeyEndpointBase(
        ILogger logger,
        UserConfigurationProvider userProvider,
        IPersonalAccessTokenService tokenService,
        IAgentKeyService agentKeyService)
    {
        _logger = logger;
        _userProvider = userProvider;
        _tokenService = tokenService;
        _agentKeyService = agentKeyService;
    }


    /// <remarks>
    /// No permission policy: an account manages its OWN keys, and a member holds no users:write.
    /// Requiring it would refuse self-service to exactly the people the route exists for. Who may
    /// act on whose keys is <see cref="AccountAuthority.ManagesKeys"/>, which the handler asks — self, the
    /// account's home-tenant administrator, or a platform administrator.
    /// </remarks>
    /// <inheritdoc />
    public override void Configure()
    {
        Delete("/users/{IdOrName}/keys/{KeyId}");
        Summary(s =>
        {
            s.Summary = "Revoke an API key";
            s.Description = "Revokes a user or agent key. The kind says which store holds it.";
        });
        ConfigureEndpoint();
    }

    /// <summary>Override to add tags or further configuration.</summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc />
    public override async Task HandleAsync(RevokeApiKeyRequest req, CancellationToken ct)
    {
        var userResult = await UserRouteTarget
            .Resolve(_userProvider, User, req.IdOrName, ct).ConfigureAwait(false);

        if (!userResult.IsSuccess || userResult.Value is null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var user = userResult.Value;

        if (!AccountAuthority.ManagesKeys(User, user, Resolve<ISystemRoleConfiguration>()))
        {
            UserEndpointLog.ApiKeyOperationNotPermitted(_logger, req.IdOrName);
            await Send.ForbiddenAsync(ct).ConfigureAwait(false);
            return;
        }

        var isAgent = string.Equals(req.Kind, ApiKeyKinds.Agent, StringComparison.OrdinalIgnoreCase);
        if (!isAgent && !string.Equals(req.Kind, ApiKeyKinds.User, StringComparison.OrdinalIgnoreCase))
        {
            AddError("API key kind must be 'user' or 'agent'.");
            await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
            return;
        }

        var result = isAgent
            ? await _agentKeyService.DeleteKey(user.Id, req.KeyId, ct).ConfigureAwait(false)
            : await _tokenService.RevokeToken(user.Id, req.KeyId, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            AddError("The key could not be revoked.");
            await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
            return;
        }

        UserEndpointLog.ApiKeyRevoked(_logger, isAgent ? ApiKeyKinds.Agent : ApiKeyKinds.User, req.IdOrName);
        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }
}
