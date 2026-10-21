using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Claims.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Methods;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.ExternalIdentityProviders.Binding;
using Fdw.Services.Authorization;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Users;
using Fdw.Services.Users.Abstractions;
using Fdw.Services.Users.Models;
using Fdw.Services.Users.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Fdw.Services.Users.Clients.Models;
using Fdw.Services.Users.Configuration;
using Fdw.Web.RestEndpoints.ErrorMapping;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Generic base endpoint for creating a new user.
/// </summary>
/// <typeparam name="TRequest">The request type, host-extensible beyond <see cref="CreateUserRequest"/>.</typeparam>
public abstract class CreateUserEndpointBase<TRequest> : Endpoint<TRequest, UserResponse>
    where TRequest : CreateUserRequest
{
    /// <summary>Initializes a new instance of the <see cref="CreateUserEndpointBase{TRequest}"/> class.</summary>
        private readonly UserConfigurationProvider _userProvider;
    private readonly UserTenantConfigurationProvider _tenantProvider;
    private readonly UserRoleConfigurationProvider _userRoleProvider;
    private readonly RoleConfigurationProvider _roleProvider;
    private readonly IUserCredentialService _credentialService;
    private readonly IPersonalAccessTokenService _tokenService;
    private readonly IAgentKeyService _agentKeyService;
    private readonly IExternalIdentityConfigurationProvider _externalIdentities;

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="CreateUserEndpointBase{TRequest}"/> class.</summary>
    protected CreateUserEndpointBase(ILogger logger, UserConfigurationProvider userProvider,
        UserTenantConfigurationProvider tenantProvider,
        UserRoleConfigurationProvider userRoleProvider,
        RoleConfigurationProvider roleProvider,
        IUserCredentialService credentialService,
        IPersonalAccessTokenService tokenService,
        IAgentKeyService agentKeyService,
        IExternalIdentityConfigurationProvider externalIdentities)
    {
        EndpointLogger = logger;
        _userProvider = userProvider;
        _tenantProvider = tenantProvider;
        _userRoleProvider = userRoleProvider;
        _roleProvider = roleProvider;
        _credentialService = credentialService;
        _tokenService = tokenService;
        _agentKeyService = agentKeyService;
        _externalIdentities = externalIdentities;
    }


    /// <summary>
    /// Gets the user provider.
    /// </summary>
    protected UserConfigurationProvider UserProvider => _userProvider;

    /// <summary>
    /// Gets the tenant provider.
    /// </summary>
    protected UserTenantConfigurationProvider TenantProvider => _tenantProvider;

    /// <summary>
    /// Gets the user-role configuration provider.
    /// </summary>
    protected UserRoleConfigurationProvider UserRoleProvider => _userRoleProvider;

    /// <summary>
    /// Gets the role configuration provider.
    /// </summary>
    protected RoleConfigurationProvider RoleProvider => _roleProvider;

    /// <summary>
    /// Gets the RBAC policy required by this endpoint. Defaults to "users:write".
    /// </summary>
    protected virtual string WritePolicy => "users:write";

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/users");
        Policies(WritePolicy);
        ConfigureEndpoint();
    }

    /// <summary>
    /// Override to configure endpoint-specific settings (auth, summary, etc.).
    /// </summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(TRequest req, CancellationToken ct)
    {
        
        OnCreatingUser(req.Username);

        var result = await Create(req, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            await SendRefusal(result, ct).ConfigureAwait(false);
            return;
        }

        if (result.Value is null)
        {
            await Send.ResultAsync(TypedResults.Json(new
            {
                errorCode = "CreateUserFailed",
                messages = OutcomeMissingMessage
            }, statusCode: 500)).ConfigureAwait(false);
            return;
        }

        OnUserCreated(req.Username, result.Value.UserId);

        var loadResult = await _userProvider.Get(result.Value.UserId, ct).ConfigureAwait(false);
        if (!loadResult.IsSuccess || loadResult.Value is null)
        {
            await Send.ResultAsync(TypedResults.Json(new
            {
                errorCode = "CreateUserPostLoadFailed",
                messages = new[] { $"User '{req.Username}' was created (id {result.Value.UserId}) but could not be re-read for the response body." }
            }, statusCode: 500)).ConfigureAwait(false);
            return;
        }

        var response = MapToResponse(loadResult.Value);
        response.ApiKey = result.Value.ApiKey;
        await Send.ResponseAsync(response, 201, ct).ConfigureAwait(false);
    }

    // Why two shapes: a request refused before anything was written is the caller's to fix
    // and answers 400. A step that failed after the account row exists is the platform's,
    // and the caller needs the account's id and what landed to finish it -- that is a 500
    // carrying both, never a 400 that reads as "nothing happened".
    private async Task SendRefusal(IGenericResult<UserCreated> result, CancellationToken ct)
    {
        var details = result.Details?.Data;
        var partial = details is not null && details.ContainsKey("userId");

        if (!partial && await SendConflict(result, ct).ConfigureAwait(false))
            return;

        if (!partial && await SendForbidden(result).ConfigureAwait(false))
            return;

        await Send.ResultAsync(TypedResults.Json(new
        {
            errorCode = partial ? "UserPartiallyCreated" : "CreateUserFailed",
            messages = new[] { result.CurrentMessage ?? "Failed to create user" },
            userId = partial ? details!["userId"] : null,
            created = partial ? details!["created"] : null,
            failed = partial ? details!["failed"] : null
        }, statusCode: partial ? 500 : 400)).ConfigureAwait(false);
    }

    // A refusal in the Forbidden band (the caller may not give a role, FDW-1577) answers 403 with its own code, not the
    // general 400: the request is well formed and the caller is signed in. Returns whether one was sent.
    private async Task<bool> SendForbidden(IGenericResult<UserCreated> result)
    {
        var (statusCode, problem) = ResultHttpStatusMapper.Map(result, HttpContext);
        if (statusCode != StatusCodes.Status403Forbidden)
            return false;

        await Send.ResultAsync(TypedResults.Json(problem, statusCode: statusCode)).ConfigureAwait(false);
        return true;
    }

    // The refusals that answer 409, each scoped to its own field so a client can tell them apart.
    // Returns whether one was sent.
    private async Task<bool> SendConflict(IGenericResult<UserCreated> result, CancellationToken ct)
    {
        // A taken username is a conflict (FDW-1056): 409, scoped to Username so a client can tell it
        // from the Roles refusal below. Names match ignoring case, so 'Jane' is taken by 'jane'.
        if (result.CodeChain.Contains(UserResultCodes.ByName("UserAlreadyExists")))
        {
            AddError(r => r.Username, "A user with this username already exists. Usernames are matched ignoring case, so a name that differs only by case is the same user.");
            await Send.ErrorsAsync(409, ct).ConfigureAwait(false);
            return true;
        }

        // Refused for the account's roles rather than its input (FDW-910): 409, as set- and
        // reset-password answer the same refusal for an existing account. Scoped to Roles, because
        // the taken-username refusal above also answers 409 and a client has to tell them apart.
        if (result.CodeChain.Contains(UserResultCodes.ByName("PasswordNotUsable")))
        {
            AddError(r => r.Roles!, "None of these roles allows password sign-in: no enabled password sign-in flow admits an account holding them. Add a role that carries what the password flow requires, or create the account without a password.");
            await Send.ErrorsAsync(409, ct).ConfigureAwait(false);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Maps the created user, as re-read from the store, to a UserResponse.
    /// Override for app-specific mapping.
    /// </summary>
    /// <remarks>
    /// This took the requested roles and echoed them back, on the argument that the store may not
    /// yet expose them. That reports what was asked for as though it were what happened: a role
    /// assignment that failed still appeared in the 201. The response now carries only what was
    /// re-read, and roles are answered by the route that owns them.
    /// </remarks>
    protected virtual UserResponse MapToResponse(IUserImplementationConfiguration user)
        => new()
        {
            Id = user.Id,
            Name = user.Name,
            Username = user.Name,
            DisplayName = user.Name,
            Email = user.Email,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            CreatedBy = HttpContext.User.Identity?.Name ?? "system",
            LastLoginAt = user.LastLoginAt
        };

    /// <summary>
    /// Performs the user creation. Override to customize creation logic.
    /// </summary>
    // The domain and implementation a user row is written under -- values the seed already writes
    // (usr.Users Domain/Implementation = 'Users'), not names this endpoint chooses.
    private const string UserDomain = "Users";
    private const string UserImplementation = "Users";
    private const string UserTenantDomain = "UserTenants";
    private const string UserTenantImplementation = "UserTenants";
    private const string UserRoleDomain = "UserRole";
    private const string ExternalIdentityDomain = "ExternalIdentity";

    private static readonly string[] OutcomeMissingMessage = ["The create reported success without an outcome."];
    private const string UserRoleImplementation = "UserRole";

    protected virtual async Task<IGenericResult<UserCreated>> Create(TRequest request, CancellationToken ct)
    {
        var admitted = await Admit(request.Username, ct).ConfigureAwait(false);
        if (admitted.IsFailure)
            return admitted.ToNewResult<UserCreated>();

        var tenantId = admitted.Value;
        var methods = request.SignInMethods;

        var usable = await MethodsAreUsable(methods, ct).ConfigureAwait(false);
        if (usable.IsFailure)
            return usable.ToNewResult<UserCreated>();

        // Why before anything is written: the caller may give a new account only the roles whose rights they hold
        // themselves (FDW-1577), the same rule as assigning a role to an existing one. An empty list gives nothing.
        if (request.Roles!.Count > 0)
        {
            var allowed = await Resolve<IGrantRule>().CanGive([.. request.Roles], tenantId, ct).ConfigureAwait(false);
            if (allowed.IsFailure)
                return allowed.ToNewResult<UserCreated>();
        }

        // Validated present before the handler runs; an empty list is an account with no roles.
        var passwordUsable = await PasswordIsUsable(request.Username, methods, request.Roles!, ct).ConfigureAwait(false);
        if (passwordUsable.IsFailure)
            return passwordUsable.ToNewResult<UserCreated>();

        // Name, Domain and Implementation are not set here: Save stamps them from its arguments,
        // and the implementation row does not persist them -- they belong to the domain row.
        var user = new UserImplementationConfiguration
        {
            Id = Guid.CreateVersion7(),
            Email = request.Email,
            TenantId = tenantId,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var createResult = await _userProvider
            .Save(user, UserDomain, UserImplementation, request.Username, ct)
            .ConfigureAwait(false);
        if (!createResult.IsSuccess)
            return createResult.ToNewResult<UserCreated>();

        var userId = user.Id;
        var outcome = new UserCreated { UserId = userId };
        outcome.Created.Add("user");

        var tenantLink = new UserTenantImplementationConfiguration
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            TenantId = tenantId,
            IsDefault = true,
        };

        // NAME MISMATCH, deliberately visible: the seed writes {username}:{tenantSlug}, and this
        // endpoint holds the tenant's id rather than its slug. Reconciling the two needs a tenant
        // read this endpoint does not have, so a row granted here and one seeded are not the same row.
        var grantResult = await _tenantProvider
            .Save(tenantLink, UserTenantDomain, UserTenantImplementation, $"{request.Username}:{tenantId}", ct)
            .ConfigureAwait(false);
        if (!grantResult.IsSuccess)
            return Partial(grantResult, outcome, "membership");

        outcome.Created.Add("membership");

        // Validated present before the handler runs; an empty list is an account with no roles.
        foreach (var roleName in request.Roles!)
        {
            var assignResult = await AssignRole(userId, roleName, tenantId, ct).ConfigureAwait(false);
            if (!assignResult.IsSuccess)
                return Partial(assignResult, outcome, "roles");
        }

        outcome.Created.Add("roles");

        return await CreateSignInMethods(request.Username, methods, outcome, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Admits a create before anything is written: the caller must carry a tenant, and the username
    /// must not already be taken (FDW-1056).
    /// </summary>
    /// <remarks>
    /// <c>Save</c> is an upsert by name, so a create that skipped the second check would write a new
    /// version of the existing account and attach the new sign-in methods to it. The name is looked up
    /// the way every other lookup does it, through <c>Get(name)</c> against the store's case-insensitive
    /// collation, so a name that differs only by case is the same account and is refused the same way.
    /// </remarks>
    /// <param name="username">The requested username.</param>
    /// <param name="ct">Cancels the lookup.</param>
    /// <returns>The caller's tenant id, or the refusal.</returns>
    private async Task<IGenericResult<Guid>> Admit(string username, CancellationToken ct)
    {
        var tenantClaim = HttpContext.User.FindFirst(ClaimDefinitions.tenantId.Name)?.Value;
        if (string.IsNullOrEmpty(tenantClaim) || !Guid.TryParse(tenantClaim, out var tenantId))
            return GenericResult<Guid>.Failure(UserResultCodes.ByName("MissingTenantClaim"));

        var existing = await _userProvider.Get(username, ct).ConfigureAwait(false);
        if (!existing.IsSuccess)
            return existing.ToNewResult<Guid>();

        return existing.Value is null
            ? GenericResult<Guid>.Success(tenantId)
            : GenericResult<Guid>.Failure(UserResultCodes.ByName("UserAlreadyExists"));
    }

    /// <summary>
    /// Answers whether the requested sign-in methods could work at all, before anything is written.
    /// </summary>
    /// <remarks>
    /// Everything checkable is checked here, so the ordinary refusal costs nothing: an account with
    /// no way in, a key kind that authenticates as nobody, or an issuer this platform cannot resolve
    /// — the last of which would otherwise write a binding that reads back and never matches a
    /// sign-in.
    /// </remarks>
    /// <summary>
    /// Refuses, before anything is written, a password for an account whose roles no enabled password
    /// flow admits (FDW-910) -- the rule set- and reset-password apply to an existing account.
    /// </summary>
    /// <remarks>
    /// The account does not exist yet, so its permissions are those of the roles it is about to be
    /// given, expanded the same way a token's are. Nothing is granted to make a password usable: that
    /// would be a privilege the caller never asked for.
    /// </remarks>
    private async Task<IGenericResult> PasswordIsUsable(
        string username, SignInMethodsRequest methods, IList<string> roles, CancellationToken ct)
    {
        if (methods.Password is null)
            return GenericResult.Success();

        IReadOnlyCollection<string> held = [];
        if (roles.Count > 0)
        {
            var granted = await Resolve<IRoleGrants>().Get([.. roles], ct).ConfigureAwait(false);
            if (granted.IsFailure || granted.Value is null)
                return (IGenericResult)granted;
            held = granted.Value;
        }

        var usable = await Resolve<ISignInMethodAdmission>().MayUse(held, PasswordAdmission.Method, ct).ConfigureAwait(false);
        if (usable.IsFailure)
            return usable;

        if (usable.Value)
            return GenericResult.Success();

        UserEndpointLog.PasswordNotUsable(EndpointLogger, "create an account with a password", username);
        return GenericResult.Failure(UserResultCodes.ByName("PasswordNotUsable"));
    }

    private async Task<IGenericResult> MethodsAreUsable(SignInMethodsRequest methods, CancellationToken ct)
    {
        if (methods.Password is null && methods.External is null && methods.ApiKey is null)
            return GenericResult.Failure(UserResultCodes.ByName("NoSignInMethod"));

        if (methods.ApiKey is not null
            && !string.Equals(methods.ApiKey.Kind, ApiKeyKinds.User, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(methods.ApiKey.Kind, ApiKeyKinds.Agent, StringComparison.OrdinalIgnoreCase))
        {
            return GenericResult.Failure(UserResultCodes.ByName("UnknownApiKeyKind"));
        }

        if (methods.External is null)
            return GenericResult.Success();

        var issuers = await Resolve<IExternalIssuerCatalog>().Read(ct).ConfigureAwait(false);
        if (issuers.IsFailure || issuers.Value is null)
            return (IGenericResult)issuers;

        if (issuers.Value.Knows(methods.External.Issuer))
            return GenericResult.Success();

        // An incomplete set cannot justify "unknown" -- the issuer may live on the one authority
        // that would not answer -- so the answer becomes "cannot determine", which is retryable.
        return issuers.Value.IsComplete
            ? GenericResult.Failure(UserResultCodes.ByName("UnknownExternalIssuer"))
            : GenericResult.Failure(UserResultCodes.ByName("ExternalIssuersUnreadable"));
    }

    /// <summary>
    /// Creates each sign-in method the request asked for, in a fixed order, and stops at the first
    /// that fails.
    /// </summary>
    /// <remarks>
    /// Password first, because it is the method most requests carry; the key last, because its raw
    /// value exists only in the response and re-minting one is cheaper than re-creating an account.
    /// </remarks>
    private async Task<IGenericResult<UserCreated>> CreateSignInMethods(
        string username, SignInMethodsRequest methods, UserCreated outcome, CancellationToken ct)
    {
        if (methods.Password is not null)
        {
            var storeResult = await _credentialService
                .Store(outcome.UserId, "Password", methods.Password.InitialPassword, ct).ConfigureAwait(false);
            if (!storeResult.IsSuccess)
                return Partial(storeResult, outcome, "password");

            // Why the flag after the store: Store clears it, and a password an administrator chose
            // is not one the account's owner has chosen yet.
            var forceResult = await _credentialService
                .ForcePasswordChange(outcome.UserId, ct).ConfigureAwait(false);
            if (!forceResult.IsSuccess)
                return Partial(forceResult, outcome, "password");

            outcome.Created.Add("password");
        }

        if (methods.External is not null)
        {
            var bindResult = await BindExternalIdentity(outcome.UserId, methods.External, ct).ConfigureAwait(false);
            if (!bindResult.IsSuccess)
                return Partial(bindResult, outcome, "external");

            outcome.Created.Add("external");
        }

        if (methods.ApiKey is not null)
        {
            var keyResult = await MintApiKey(outcome.UserId, username, methods.ApiKey, ct).ConfigureAwait(false);
            if (!keyResult.IsSuccess || keyResult.Value is null)
                return Partial(keyResult, outcome, "apiKey");

            outcome.ApiKey = keyResult.Value;
            outcome.Created.Add("apiKey");
        }

        return GenericResult<UserCreated>.Success(outcome);
    }

    /// <summary>
    /// Reports a step that failed after the account row already exists.
    /// </summary>
    /// <remarks>
    /// The account is in ConfigurationDb and the credentials are in AuthDb, so there is no single
    /// transaction to roll back. Saying which step failed, and naming what already landed, is what
    /// lets the caller finish through the per-method routes rather than create the account twice.
    /// </remarks>
    private static GenericResult<UserCreated> Partial(IGenericResult failed, UserCreated outcome, string step)
        => (GenericResult<UserCreated>)GenericResult<UserCreated>.Failure(
            UserResultCodes.ByName("UserPartiallyCreated"),
            ResultDetails.Create(
                "userId", outcome.UserId,
                "created", string.Join(",", outcome.Created),
                "failed", step + ": " + (failed.CurrentMessage ?? "no reason given")));

    /// <summary>
    /// Binds an external identity to the account before its first sign-in, so that sign-in resolves
    /// here instead of provisioning a second account.
    /// </summary>
    /// <remarks>
    /// The subject is what the issuer asserts. When only an email is known an invitation is written
    /// against the email and the issuer's subject is recorded the first time it signs in (FDW-910).
    /// </remarks>
    private async Task<IGenericResult> BindExternalIdentity(
        Guid userId, ExternalSignInMethod method, CancellationToken ct)
    {
        var row = ExternalIdentityRow.For(userId, method.Issuer, method.Subject, method.InvitedEmail);
        return await _externalIdentities.Save(row, ExternalIdentityDomain, ExternalIdentityDomain, row.Name, ct)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Mints the account's first API key. The raw key is returned once, in the create response.
    /// </summary>
    private async Task<IGenericResult<ApiKeyIssued>> MintApiKey(
        Guid userId, string username, ApiKeySignInMethod method, CancellationToken ct)
    {
        if (string.Equals(method.Kind, ApiKeyKinds.Agent, StringComparison.OrdinalIgnoreCase))
        {
            var agent = await _agentKeyService
                .CreateKey(userId, username, method.Label, method.ExpiresAt, ct).ConfigureAwait(false);
            if (!agent.IsSuccess || agent.Value is null)
                return agent.ToNewResult<ApiKeyIssued>();

            return GenericResult<ApiKeyIssued>.Success(new ApiKeyIssued
            {
                KeyId = agent.Value.KeyId,
                Kind = ApiKeyKinds.Agent,
                Label = agent.Value.Label,
                RawKey = agent.Value.RawKey,
                Prefix = agent.Value.Prefix,
                ExpiresAt = method.ExpiresAt,
            });
        }

        var token = await _tokenService
            .CreateToken(userId, method.Label, method.ExpiresAt, ct).ConfigureAwait(false);
        if (!token.IsSuccess || token.Value is null)
            return token.ToNewResult<ApiKeyIssued>();

        return GenericResult<ApiKeyIssued>.Success(new ApiKeyIssued
        {
            KeyId = token.Value.TokenId,
            Kind = ApiKeyKinds.User,
            Label = token.Value.Label,
            RawKey = token.Value.RawToken,
            Prefix = token.Value.Prefix,
            ExpiresAt = method.ExpiresAt,
        });
    }

    private async Task<IGenericResult> AssignRole(Guid userId, string roleName, Guid tenantId, CancellationToken ct)
    {
        var roleResult = await _roleProvider.Get(roleName, ct).ConfigureAwait(false);
        if (!roleResult.IsSuccess || roleResult.Value is null)
            return GenericResult.Failure(UserEndpointLog.RoleNotFoundDuringCreate(EndpointLogger, roleName));

        var role = roleResult.Value;

        var config = new UserRoleImplementationConfiguration
        {
            Id = Guid.CreateVersion7(),
            UserId = userId.ToString(),
            RoleId = role.Id,
            // Granted in the new user's home tenant, the one they were just made a member of: roles
            // are held per tenant (FDW-825), and a role with no tenant is one no token ever carries.
            TenantId = tenantId,
            AssignedAt = DateTimeOffset.UtcNow
        };

        // {userId}:{roleName} is what the seed writes into authz.UserRole.[Name]. Keyed on the role's
        // Id instead, a row written here is invisible to the seed's own idempotence check.
        return await _userRoleProvider
            .Save(config, UserRoleDomain, UserRoleImplementation, $"{userId}:{role.Name}", ct)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Called when creating a user. Override for custom logging.
    /// </summary>
    protected virtual void OnCreatingUser(string username)
    {
    }

    /// <summary>
    /// Called when a user has been created. Override for custom logging.
    /// </summary>
    protected virtual void OnUserCreated(string username, Guid userId)
    {
    }
}
