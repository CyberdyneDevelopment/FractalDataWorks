using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Methods;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Users.Clients.Models;
using Fdw.Services.Users.Configuration;
using Microsoft.Extensions.Logging;
using Fdw.Web.RestEndpoints.ErrorMapping;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Generic base endpoint minting an API key for an account.
/// </summary>
/// <remarks>
/// One route for both kinds of key. A console shows a person one list of "API keys", and the kind
/// decides which store holds the key and which principal it authenticates as — that is a property
/// of the key, not a reason for a second resource.
/// </remarks>
public abstract class CreateApiKeyEndpointBase : Endpoint<CreateApiKeyRequest, ApiKeyIssued>
{
    private readonly IUserConfigurationProvider _userProvider;
    private readonly IPersonalAccessTokenService _tokenService;
    private readonly IAgentKeyService _agentKeyService;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="CreateApiKeyEndpointBase"/> class.</summary>
    protected CreateApiKeyEndpointBase(
        ILogger logger,
        IUserConfigurationProvider userProvider,
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
        Post("/users/{IdOrName}/keys");
        Summary(s =>
        {
            s.Summary = "Mint an API key";
            s.Description = "Mints a user or agent key. The raw key is in this response and nowhere else.";
        });
        ConfigureEndpoint();
    }

    /// <summary>Override to add tags or further configuration.</summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CreateApiKeyRequest req, CancellationToken ct)
    {
        var userResult = await UserRouteTarget
            .Resolve(_userProvider, User, req.IdOrName, ct).ConfigureAwait(false);

        if (userResult.IsFailure)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(userResult, HttpContext)).ConfigureAwait(false);
            return;
        }

        if (userResult.Value is null)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(UserEndpointLog.CreateApiKeyUserNotFound(_logger)), HttpContext)).ConfigureAwait(false);
            return;
        }

        var user = userResult.Value;

        if (!AccountAuthority.ManagesKeys(User, user, Resolve<ISystemRoleConfiguration>()))
        {
            UserEndpointLog.ApiKeyOperationNotPermitted(_logger, req.IdOrName);
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(UserEndpointLog.CreateApiKeyForbidden(_logger)), HttpContext)).ConfigureAwait(false);
            return;
        }

        var isAgent = string.Equals(req.Kind, ApiKeyKinds.Agent, StringComparison.OrdinalIgnoreCase);
        if (!isAgent && !string.Equals(req.Kind, ApiKeyKinds.User, StringComparison.OrdinalIgnoreCase))
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(UserEndpointLog.CreateApiKeyApiKeyKindMustBe(_logger)), HttpContext)).ConfigureAwait(false);
            return;
        }

        var issued = isAgent
            ? await MintAgentKey(user, req, ct).ConfigureAwait(false)
            : await MintUserKey(user, req, ct).ConfigureAwait(false);

        if (!issued.IsSuccess || issued.Value is null)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(issued.Messages.Append(UserEndpointLog.CreateApiKeyReturnedNothing(_logger))), HttpContext)).ConfigureAwait(false);
            return;
        }

        UserEndpointLog.ApiKeyMinted(_logger, issued.Value.Kind, req.IdOrName);
        await Send.ResponseAsync(issued.Value, 201, ct).ConfigureAwait(false);
    }

    private async Task<IGenericResult<ApiKeyIssued>> MintAgentKey(
        IUserImplementationConfiguration user, CreateApiKeyRequest req, CancellationToken ct)
    {
        var created = await _agentKeyService
            .CreateKey(user.Id, user.Name, req.Label, req.ExpiresAt, ct).ConfigureAwait(false);
        if (!created.IsSuccess || created.Value is null)
            return created.ToNewResult<ApiKeyIssued>();

        return GenericResult<ApiKeyIssued>.Success(new ApiKeyIssued
        {
            KeyId = created.Value.KeyId,
            Kind = ApiKeyKinds.Agent,
            Label = created.Value.Label,
            RawKey = created.Value.RawKey,
            Prefix = created.Value.Prefix,
            ExpiresAt = req.ExpiresAt,
        });
    }

    private async Task<IGenericResult<ApiKeyIssued>> MintUserKey(
        IUserImplementationConfiguration user, CreateApiKeyRequest req, CancellationToken ct)
    {
        var created = await _tokenService
            .CreateToken(user.Id, req.Label, req.ExpiresAt, ct).ConfigureAwait(false);
        if (!created.IsSuccess || created.Value is null)
            return created.ToNewResult<ApiKeyIssued>();

        return GenericResult<ApiKeyIssued>.Success(new ApiKeyIssued
        {
            KeyId = created.Value.TokenId,
            Kind = ApiKeyKinds.User,
            Label = created.Value.Label,
            RawKey = created.Value.RawToken,
            Prefix = created.Value.Prefix,
            ExpiresAt = req.ExpiresAt,
        });
    }
}
