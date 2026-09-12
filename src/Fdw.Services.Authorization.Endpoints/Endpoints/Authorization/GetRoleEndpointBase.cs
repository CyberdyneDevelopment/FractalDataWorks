using System;
using Fdw.Results;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Fdw.Services.Authorization.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>
/// Generic base endpoint for getting a role by name.
/// </summary>
public abstract class GetRoleEndpointBase : Endpoint<GetRoleRequest, RoleDetailResponse>
{
    /// <summary>Initializes a new instance of the <see cref="GetRoleEndpointBase"/> class.</summary>
        private readonly IRoleConfigurationProvider _roleProvider;
    private readonly IPermissionConfigurationProvider _permissionProvider;
    private readonly IRolePermissionConfigurationProvider _rolePermissionProvider;

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="GetRoleEndpointBase"/> class.</summary>
    protected GetRoleEndpointBase(
        ILogger logger,
        IRoleConfigurationProvider roleProvider,
        IPermissionConfigurationProvider permissionProvider,
        IRolePermissionConfigurationProvider rolePermissionProvider)
    {
        EndpointLogger = logger;
        _roleProvider = roleProvider;
        _permissionProvider = permissionProvider;
        _rolePermissionProvider = rolePermissionProvider;
    }


    /// <summary>
    /// Gets the RBAC policy required by this endpoint. Defaults to "settings/role:read".
    /// </summary>
    protected virtual string ReadPolicy => "settings/role:read";

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/roles/{Name}");
        Policies(ReadPolicy);
        ConfigureEndpoint();
    }

    /// <summary>
    /// Override to configure endpoint-specific settings (auth, summary, etc.).
    /// </summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(GetRoleRequest req, CancellationToken ct)
    {

        var roleResult = Guid.TryParse(req.Name, out var id)
            ? await _roleProvider.Get(id, ct).ConfigureAwait(false)
            : await _roleProvider.Get(req.Name, ct).ConfigureAwait(false);

        if (!roleResult.IsSuccess)
        {
            AuthorizationEndpointLog.AuthorizationReadFailed(EndpointLogger, req.Name,
                roleResult.CurrentMessage);
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        if (roleResult.Value is null)
        {
            HttpContext.Response.StatusCode = 404;
            HttpContext.Response.ContentType = "application/json";
            await HttpContext.Response.WriteAsJsonAsync(
                new { errorCode = "NotFound", messages = new[] { $"roles '{req.Name}' was not found." } }, ct).ConfigureAwait(false);
            return;
        }

        var response = await MapToDetail(roleResult.Value, ct).ConfigureAwait(false);
        if (response.IsFailure || response.Value is null)
        {
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(response.Value, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Maps a role implementation configuration to a detail DTO. Override for custom mapping.
    /// </summary>
    /// <remarks>
    /// Returns a result rather than writing to the response: HandleAsync owns every send, so a
    /// mapper that failed could not also have answered the caller.
    /// </remarks>
    protected virtual async Task<IGenericResult<RoleDetailResponse>> MapToDetail(IRoleImplementationConfiguration role, CancellationToken ct)
    {
        var rolePermissionsResult = await _rolePermissionProvider
            .Find<IRolePermissionImplementationConfiguration>(rp => rp.RoleId == role.Id, ct).ConfigureAwait(false);
        if (!rolePermissionsResult.IsSuccess || rolePermissionsResult.Value is null)
        {
            return GenericResult<RoleDetailResponse>.Failure(
                AuthorizationEndpointLog.AuthorizationReadFailed(EndpointLogger, role.Name,
                    rolePermissionsResult.CurrentMessage));
        }
        var rolePermissions = rolePermissionsResult.Value;

        var allPermissionsResult = await _permissionProvider.Get(ct).ConfigureAwait(false);
        if (!allPermissionsResult.IsSuccess || allPermissionsResult.Value is null)
        {
            return GenericResult<RoleDetailResponse>.Failure(
                AuthorizationEndpointLog.AuthorizationReadFailed(EndpointLogger, role.Name,
                    allPermissionsResult.CurrentMessage));
        }
        var allPermissions = allPermissionsResult.Value;

        var permissions = allPermissions
            .Where(p => rolePermissions.Any(rp => rp.PermissionId == p.Id))
            .Select(p => new PermissionSummaryDto
            {
                Id = p.Id,
                Name = p.Name,
                DisplayName = p.DisplayName,
                Description = p.Description,
                Domain = p.Domain,
                Resource = p.Resource,
                Action = p.Action,
                Scope = p.Scope
            })
            .ToList();

        return GenericResult<RoleDetailResponse>.Success(new RoleDetailResponse
        {
            Id = role.Id,
            Name = role.Name,
            DisplayName = role.DisplayName,
            Description = role.Description,
            IsTenantScoped = role.IsTenantScoped,
            SortOrder = role.SortOrder,
            Permissions = permissions,
            CreatedAt = role.CreateDate
        });
    }
}
