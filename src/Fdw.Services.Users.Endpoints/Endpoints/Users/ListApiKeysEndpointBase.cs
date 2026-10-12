using System;
using System.Collections.Generic;
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
/// Generic base endpoint listing an account's API keys, both kinds in one list.
/// </summary>
public abstract class ListApiKeysEndpointBase : Endpoint<UserScopedRequest, List<ApiKeyState>>
{
    private readonly UserConfigurationProvider _userProvider;
    private readonly IPersonalAccessTokenService _tokenService;
    private readonly IAgentKeyService _agentKeyService;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="ListApiKeysEndpointBase"/> class.</summary>
    protected ListApiKeysEndpointBase(
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
        Get("/users/{IdOrName}/keys");
        Summary(s =>
        {
            s.Summary = "List an account's API keys";
            s.Description = "User keys and agent keys in one list. Raw keys are never listed — only their prefix.";
        });
        ConfigureEndpoint();
    }

    /// <summary>Override to add tags or further configuration.</summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc />
    public override async Task HandleAsync(UserScopedRequest req, CancellationToken ct)
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

        var keys = new List<ApiKeyState>();

        var tokens = await _tokenService.ListTokens(user.Id, ct).ConfigureAwait(false);
        if (tokens.IsSuccess && tokens.Value is not null)
        {
            foreach (var token in tokens.Value)
            {
                keys.Add(new ApiKeyState
                {
                    KeyId = token.TokenId,
                    Kind = ApiKeyKinds.User,
                    Label = token.Label,
                    Prefix = token.Prefix,
                    CreatedAt = token.CreatedAt,
                    ExpiresAt = token.ExpiresAt,
                    LastUsedAt = token.LastUsedAt,
                });
            }
        }

        var agentKeys = await _agentKeyService.ListKeys(user.Id, ct).ConfigureAwait(false);
        if (agentKeys.IsSuccess && agentKeys.Value is not null)
        {
            foreach (var key in agentKeys.Value)
            {
                keys.Add(new ApiKeyState
                {
                    KeyId = key.KeyId,
                    Kind = ApiKeyKinds.Agent,
                    Label = key.Label,
                    Prefix = key.Prefix,
                    CreatedAt = key.CreatedAt,
                    ExpiresAt = key.ExpiresAt,
                    LastUsedAt = key.LastUsedAt,
                });
            }
        }

        await Send.OkAsync(keys, ct).ConfigureAwait(false);
    }
}
