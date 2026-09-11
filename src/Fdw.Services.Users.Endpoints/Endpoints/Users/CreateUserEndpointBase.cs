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
using Fdw.Services.Users.Configuration;

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

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="CreateUserEndpointBase{TRequest}"/> class.</summary>
    protected CreateUserEndpointBase(ILogger logger, UserConfigurationProvider userProvider,
        UserTenantConfigurationProvider tenantProvider,
        UserRoleConfigurationProvider userRoleProvider,
        RoleConfigurationProvider roleProvider,
        IUserCredentialService credentialService)
    {
        EndpointLogger = logger;
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

        var loadResult = await _userProvider.Get(result.Value, ct).ConfigureAwait(false);
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

        await Send.ResponseAsync(MapToResponse(loadResult.Value), 201, ct).ConfigureAwait(false);
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
    private const string UserRoleImplementation = "UserRole";

    protected virtual async Task<IGenericResult<Guid>> Create(TRequest request, CancellationToken ct)
    {
        var tenantClaim = HttpContext.User.FindFirst(ClaimDefinitions.tenantId.Name)?.Value;
        if (string.IsNullOrEmpty(tenantClaim) || !Guid.TryParse(tenantClaim, out var tenantId))
        {
            return GenericResult<Guid>.Failure(UserResultCodes.ByName("MissingTenantClaim"));
        }

        // Name, Domain and Implementation are not set here: Save stamps them from its arguments,
        // and the implementation row does not persist them -- they belong to the domain row.
        var user = new UserImplementationConfiguration
        {
            Id = Guid.CreateVersion7(),
            Email = request.Email,
            TenantId = tenantId,
            IsActive = true,
        };

        var createResult = await _userProvider
            .Save(user, UserDomain, UserImplementation, request.Username, ct)
            .ConfigureAwait(false);
        if (!createResult.IsSuccess)
            return createResult.ToNewResult<Guid>();

        var userId = user.Id;

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

        return GenericResult<Guid>.Success(userId);
    }

    private async Task<IGenericResult> AssignRole(Guid userId, string roleName, CancellationToken ct)
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
