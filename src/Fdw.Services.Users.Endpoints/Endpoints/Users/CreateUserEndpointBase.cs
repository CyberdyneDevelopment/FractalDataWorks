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
            CreatedBy = HttpContext.User.Identity?.Name ?? "system",
            LastLoginAt = user.LastLoginAt
        };

    /// <summary>
    /// Performs the user creation. Override to customize creation logic.
    /// </summary>
    protected virtual async Task<IGenericResult<Guid>> Create(TRequest request, CancellationToken ct)
    {
        var tenantClaim = HttpContext.User.FindFirst(ClaimDefinitions.tenantId.Name)?.Value;
        if (string.IsNullOrEmpty(tenantClaim) || !Guid.TryParse(tenantClaim, out var tenantId))
        {
            return GenericResult<Guid>.Failure(UserResultCodes.ByName("MissingTenantClaim"));
        }

        var createResult = await _userProvider.CreateUser(request.Username, request.Email, tenantId, ct).ConfigureAwait(false);
        if (!createResult.IsSuccess)
            return createResult;

        var userId = createResult.Value;

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
