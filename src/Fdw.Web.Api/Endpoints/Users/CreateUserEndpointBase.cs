using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authorization;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Users;
using Fdw.Services.Users.Abstractions;
using Fdw.Services.Users.Models;
using Fdw.Services.Users.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Fdw.Services.Users.Clients.Models;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Generic base endpoint for creating a new user.
/// </summary>
/// <typeparam name="TRequest">The request type, host-extensible beyond <see cref="CreateUserRequest"/>.</typeparam>
public abstract class CreateUserEndpointBase<TRequest> : Endpoint<TRequest, UserResponse>
    where TRequest : CreateUserRequest
{
    private readonly UserConfigurationProvider _userProvider;
    private readonly UserTenantConfigurationProvider _tenantProvider;
    private readonly UserRoleConfigurationProvider _userRoleProvider;
    private readonly RoleConfigurationProvider _roleProvider;
    private readonly IUserCredentialService _credentialService;

    /// <summary>
    /// Gets the logger instance. Resolved during HandleAsync.
    /// </summary>
    protected ILogger EndpointLogger { get; private set; } = null!;

    /// <inheritdoc />
    protected CreateUserEndpointBase(
        UserConfigurationProvider userProvider,
        UserTenantConfigurationProvider tenantProvider,
        UserRoleConfigurationProvider userRoleProvider,
        RoleConfigurationProvider roleProvider,
        IUserCredentialService credentialService)
    {
        _userProvider = userProvider;
        _tenantProvider = tenantProvider;
        _userRoleProvider = userRoleProvider;
        _roleProvider = roleProvider;
        _credentialService = credentialService;
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
    // Why: the standard CRUD tier for this resource. This endpoint previously required ":delete"
    // as an ad-hoc "Admin-only" tier, because the seeded Operator role is granted ":write" on
    // every resource by a blanket rule and would otherwise have inherited user administration.
    // The grant was the wrong thing to work around: user/role admin is now carved out of
    // Operator in the seed, so these permissions can mean exactly what they say (FDW-634).
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
        EndpointLogger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        OnCreatingUser(req.Username);

        var result = await Create(req, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            HttpContext.Response.StatusCode = 400;
            HttpContext.Response.ContentType = "application/json";
            await HttpContext.Response.WriteAsJsonAsync(new
            {
                errorCode = "CreateUserFailed",
                messages = new[] { result.CurrentMessage ?? "Failed to create user" }
            }, ct).ConfigureAwait(false);
            return;
        }

        OnUserCreated(req.Username, result.Value);

        // Why: round-trip the just-created user so the response body is the canonical
        // resource (UserResponse) not a {success, userId, message} wrapper — per
        // API-65, create endpoints return the resource.
        var loadResult = await _userProvider.GetUser(result.Value, ct).ConfigureAwait(false);
        if (!loadResult.IsSuccess || loadResult.Value is null)
        {
            HttpContext.Response.StatusCode = 500;
            HttpContext.Response.ContentType = "application/json";
            await HttpContext.Response.WriteAsJsonAsync(new
            {
                errorCode = "CreateUserPostLoadFailed",
                messages = new[] { $"User '{req.Username}' was created (id {result.Value}) but could not be re-read for the response body." }
            }, ct).ConfigureAwait(false);
            return;
        }

        await Send.ResponseAsync(MapToResponse(loadResult.Value, req.Roles), 201, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Maps a user entity (plus the requested roles, which the store may not yet expose) to a UserResponse.
    /// Override for app-specific mapping.
    /// </summary>
    protected virtual UserResponse MapToResponse(IUser user, IList<string> requestedRoles)
        => new()
        {
            Id = user.Id,
            Name = user.Username,
            Username = user.Username,
            DisplayName = user.Username,
            Email = user.Email,
            IsActive = user.IsActive,
            Roles = requestedRoles,
            CreatedAt = user.CreatedAt,
            // Why: API-47 expects an audit trail on the create response. The creating principal is
            // on the authenticated identity; fall back to "system" when no user is present so the
            // field is never null in the contract.
            CreatedBy = HttpContext.User.Identity?.Name ?? "system",
            LastLoginAt = user.LastLoginAt
        };

    /// <summary>
    /// Performs the user creation. Override to customize creation logic.
    /// </summary>
    protected virtual async Task<IGenericResult<Guid>> Create(TRequest request, CancellationToken ct)
    {
        // Why: every user is tenant-scoped. The caller's tenant context (from the JWT tenant_id
        // claim) is the source of truth — a user creating accounts implicitly creates them inside
        // their own tenant. Missing claim → fail loud rather than silently dropping the new user
        // into the default tenant.
        var tenantClaim = HttpContext.User.FindFirst(ClaimDefinitions.tenantId.Name)?.Value;
        if (string.IsNullOrEmpty(tenantClaim) || !Guid.TryParse(tenantClaim, out var tenantId))
        {
            return GenericResult<Guid>.Failure(UserResultCodes.ByName("MissingTenantClaim"));
        }

        // Why: Create the user record first (no tenant membership yet), then grant the first
        // tenant membership as the default, then hash and store the password credential.
        // Hashing happens at the service boundary — plaintext never enters DataGateway queries.
        var createResult = await _userProvider.CreateUser(request.Username, request.Email, tenantId, ct).ConfigureAwait(false);
        if (!createResult.IsSuccess)
            return createResult;

        var userId = createResult.Value;

        // Why: API-126 — grant the first tenant membership as the default so DefaultPrincipalResolver
        // can resolve a default tenant at token-issue time. Fail loud if the grant fails.
        var grantResult = await _tenantProvider.GrantTenantAccess(userId, tenantId, isDefault: true, ct).ConfigureAwait(false);
        if (!grantResult.IsSuccess)
            return grantResult.ToNewResult<Guid>();

        var storeResult = await _credentialService.Store(userId, "Password", request.Password, ct).ConfigureAwait(false);
        if (!storeResult.IsSuccess)
            return storeResult.ToNewResult<Guid>();

        foreach (var roleName in request.Roles)
        {
            var assignResult = await AssignRole(userId, roleName, ct).ConfigureAwait(false);
            if (!assignResult.IsSuccess)
                return assignResult.ToNewResult<Guid>();
        }

        return createResult;
    }

    // Why: extracted to keep Create() below the FDW007 cyclomatic-complexity threshold.
    private async Task<IGenericResult> AssignRole(Guid userId, string roleName, CancellationToken ct)
    {
        var role = await _roleProvider.GetRole(roleName, ct).ConfigureAwait(false);
        if (role is null)
            return GenericResult.Failure(UserEndpointLog.RoleNotFoundDuringCreate(EndpointLogger, roleName));

        var config = new UserRoleConfiguration
        {
            Id = Guid.NewGuid(),
            UserId = userId.ToString(),
            RoleId = role.Id,
            Name = $"{userId}:{role.Id}",
            AssignedAt = DateTimeOffset.UtcNow
        };

        return await _userRoleProvider.Save(config, ct).ConfigureAwait(false);
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
