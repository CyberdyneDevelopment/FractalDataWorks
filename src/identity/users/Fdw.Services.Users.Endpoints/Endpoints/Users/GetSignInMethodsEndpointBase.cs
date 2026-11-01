using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Claims.Abstractions;
using Fdw.Services.Authentication.Abstractions.Methods;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.ExternalIdentityProviders.Binding;
using Fdw.Services.Users.Abstractions;
using Fdw.Services.Users.Clients.Models;
using Fdw.Services.Users.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Generic base endpoint answering every way an account can sign in.
/// </summary>
/// <remarks>
/// The three methods live in three stores — the password and the keys in AuthDb, the bindings in
/// ConfigurationDb — and a screen that manages them needs all three at once. The capability flags
/// are computed here rather than in the client, because they depend on facts a token does not
/// carry: whether a password already exists, and which tenant the account is homed in.
/// </remarks>
public abstract class GetSignInMethodsEndpointBase : Endpoint<UserScopedRequest, SignInMethodsResponse>
{
    private readonly UserConfigurationProvider _userProvider;
    private readonly IUserCredentialService _credentialService;
    private readonly IExternalIdentityConfigurationProvider _externalIdentities;
    private readonly IPersonalAccessTokenService _tokenService;
    private readonly IAgentKeyService _agentKeyService;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="GetSignInMethodsEndpointBase"/> class.</summary>
    protected GetSignInMethodsEndpointBase(
        ILogger logger,
        UserConfigurationProvider userProvider,
        IUserCredentialService credentialService,
        IExternalIdentityConfigurationProvider externalIdentities,
        IPersonalAccessTokenService tokenService,
        IAgentKeyService agentKeyService)
    {
        _logger = logger;
        _userProvider = userProvider;
        _credentialService = credentialService;
        _externalIdentities = externalIdentities;
        _tokenService = tokenService;
        _agentKeyService = agentKeyService;
    }

    /// <summary>Gets the RBAC policy required by this endpoint. Defaults to "users:read".</summary>
    protected virtual string ReadPolicy => "users:read";

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/users/{IdOrName}/sign-in-methods");
        Policies(ReadPolicy);
        Summary(s =>
        {
            s.Summary = "List an account's sign-in methods";
            s.Description = "Password state, external-identity bindings and API keys, with what the calling token may do about each.";
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

        // Row-level security hides accounts outside the caller's tenant, so "not found" also covers
        // "not yours to see".
        if (!userResult.IsSuccess || userResult.Value is null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var user = userResult.Value;
        var userId = user.Id;

        var roles = Resolve<ISystemRoleConfiguration>();
        var isPlatformAdmin = roles.IsPlatformAdmin(User);
        // The flags a console disables buttons by are the SAME rule the routes enforce, asked once
        // here. Computing them a second way is how a panel comes to promise what a route refuses.
        var administers = AccountAuthority.Administers(User, user, roles);

        var hasPassword = await _credentialService.HasCredential(userId, "Password", ct).ConfigureAwait(false);
        if (hasPassword.IsFailure)
        {
            UserEndpointLog.SignInMethodsReadFailed(_logger, req.IdOrName);
            await Send.ResponseAsync(new SignInMethodsResponse(), 500, ct).ConfigureAwait(false);
            return;
        }

        // The same question the password routes ask before issuing one (FDW-910), asked here so the
        // panel neither promises a password the route refuses nor hides why a set one does not work.
        var usable = await PasswordAdmission.Check(Resolve<ISignInMethodAdmission>(), userId, ct).ConfigureAwait(false);
        if (usable.IsFailure)
            UserEndpointLog.PasswordAdmissionUndecided(_logger, req.IdOrName);

        var response = new SignInMethodsResponse
        {
            Password = BuildPasswordState(user, hasPassword.Value, isPlatformAdmin, administers, usable.IsSuccess ? usable.Value : null),
            CanMintKeys = AccountAuthority.ManagesKeys(User, user, roles),
            CanBind = administers,
        };

        if (response.CanBind)
        {
            // The values a binding may name come from the same read that decides whether the caller
            // may bind at all, so a console cannot be told "yes, you may" and left to guess with what.
            var issuers = await Resolve<IExternalIssuerCatalog>().Read(ct).ConfigureAwait(false);
            if (issuers.IsSuccess && issuers.Value is not null)
            {
                response.BindableIssuers = [.. issuers.Value.Issuers];
                response.BindableIssuersComplete = issuers.Value.IsComplete;
            }
            else
            {
                // The read itself failed rather than one authority within it: nothing is known, and
                // saying so is different from saying this deployment federates with nobody.
                response.BindableIssuersComplete = false;
            }
        }

        await AddExternalIdentities(response, userId, ct).ConfigureAwait(false);
        await AddKeys(response, userId, ct).ConfigureAwait(false);

        await Send.OkAsync(response, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// States the password method, and who may set one.
    /// </summary>
    /// <remarks>
    /// A platform admin may always set a password. Whoever administers the account — the Admin role
    /// in the tenant it is homed in — may set the FIRST one, so a tenant can finish setting up its
    /// own service accounts, and never replaces an existing one: that is a reset, and it belongs to
    /// the platform admin alone.
    /// <para>
    /// Neither may set one the account could not use: when no enabled password flow admits it, setting
    /// is refused with the reason, the way the route refuses it (FDW-910/912).
    /// </para>
    /// </remarks>
    private static PasswordMethodState BuildPasswordState(
        IUserImplementationConfiguration user, bool hasPassword, bool isPlatformAdmin, bool administers, bool? isUsable)
    {
        var state = new PasswordMethodState
        {
            IsSet = hasPassword,
            MustChange = user.MustChangePasswordOnLogin,
            ChangedAt = user.LastPasswordChangedAt,
            IsUsable = isUsable,
            CanSet = (isPlatformAdmin || (administers && !hasPassword)) && isUsable == true,
            CanRemove = hasPassword && administers,
        };

        if (!state.CanSet)
            state.CanSetReason = CannotSetReason(hasPassword, isPlatformAdmin, administers, isUsable);

        return state;
    }

    // Authority first: a caller who may not set a password at all is told that, not something about flows.
    private static string CannotSetReason(bool hasPassword, bool isPlatformAdmin, bool administers, bool? isUsable)
    {
        if (!isPlatformAdmin && !(administers && !hasPassword))
        {
            return hasPassword
                ? "A password is already set; only a platform administrator may reset it."
                : "Only this account's home-tenant administrator or a platform administrator may set its first password.";
        }

        return isUsable == false
            ? "No enabled password sign-in flow admits this account. Give it a role that carries the permission the flow requires first."
            : "Whether this account may sign in with a password could not be decided.";
    }

    private async Task AddExternalIdentities(SignInMethodsResponse response, Guid userId, CancellationToken ct)
    {
        var bindings = await _externalIdentities
            .Find<ExternalIdentityConfiguration>(b => b.UserId == userId, ct).ConfigureAwait(false);
        if (bindings.IsFailure || bindings.Value is null)
            return;

        foreach (var binding in bindings.Value)
        {
            response.External.Add(new ExternalIdentityState
            {
                BindingId = binding.Id,
                Issuer = binding.Issuer,
                Subject = binding.ExternalSubject,
                InvitedEmail = binding.InvitedEmail,
                IsActive = binding.IsActive,
                // A binding written before the first sign-in carries the subject an administrator
                // supplied; the issuer confirms it the first time it is used. An invitation carries
                // no subject at all until the sign-in that redeems it (FDW-910).
                Status = binding.ExternalSubject is null ? "Invited" : binding.BoundAt is null ? "Pending" : "Bound",
                BoundAt = binding.BoundAt,
            });
        }
    }

    private async Task AddKeys(SignInMethodsResponse response, Guid userId, CancellationToken ct)
    {
        var tokens = await _tokenService.ListTokens(userId, ct).ConfigureAwait(false);
        if (tokens.IsSuccess && tokens.Value is not null)
        {
            foreach (var token in tokens.Value)
            {
                response.Keys.Add(new ApiKeyState
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

        var keys = await _agentKeyService.ListKeys(userId, ct).ConfigureAwait(false);
        if (keys.IsSuccess && keys.Value is not null)
        {
            foreach (var key in keys.Value)
            {
                response.Keys.Add(new ApiKeyState
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
    }
}
