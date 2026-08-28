using System;
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
    private readonly RoleConfigurationProvider _roleProvider;

    /// <summary>
    /// Gets the logger instance. Resolved during HandleAsync.
    /// </summary>
    protected ILogger EndpointLogger { get; private set; } = null!;

    /// <inheritdoc />
    protected GetRoleEndpointBase(RoleConfigurationProvider roleProvider)
    {
        _roleProvider = roleProvider;
    }

    /// <summary>
    /// Gets the role configuration provider.
    /// </summary>
    protected RoleConfigurationProvider RoleProvider => _roleProvider;

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
        EndpointLogger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        RoleConfiguration? role = Guid.TryParse(req.Name, out var id)
            ? await _roleProvider.GetRole(id, ct).ConfigureAwait(false)
            : await _roleProvider.GetRole(req.Name, ct).ConfigureAwait(false);

        if (role is null)
        {
            HttpContext.Response.StatusCode = 404;
            HttpContext.Response.ContentType = "application/json";
            await HttpContext.Response.WriteAsJsonAsync(
                new { errorCode = "NotFound", messages = new[] { $"roles '{req.Name}' was not found." } }, ct).ConfigureAwait(false);
            return;
        }

        var response = await MapToDetail(role, ct).ConfigureAwait(false);
        await Send.OkAsync(response, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Maps a RoleConfiguration to a detail DTO. Override for custom mapping.
    /// </summary>
    protected virtual async Task<RoleDetailResponse> MapToDetail(RoleConfiguration role, CancellationToken ct)
    {
        var rolePermissions = await _roleProvider.GetRolePermissions(role.Id, ct).ConfigureAwait(false);
        var allPermissions = await _roleProvider.GetPermissions(ct).ConfigureAwait(false);

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

        return new RoleDetailResponse
        {
            Id = role.Id,
            Name = role.Name,
            DisplayName = role.DisplayName,
            Description = role.Description,
            IsTenantScoped = role.IsTenantScoped,
            SortOrder = role.SortOrder,
            Permissions = permissions,
            CreatedAt = role.CreateDate
        };
    }
}
