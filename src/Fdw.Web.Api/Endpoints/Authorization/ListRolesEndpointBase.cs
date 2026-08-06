using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authorization.Configuration;
using Fdw.Web.RestEndpoints.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>
/// Generic base endpoint for listing all roles.
/// </summary>
public abstract class ListRolesEndpointBase : EndpointWithoutRequest<PaginatedResponse<RoleSummaryResponse>>
{
    private readonly IAuthorizationProvider _authorizationProvider;

    /// <summary>
    /// Gets the logger instance. Resolved during HandleAsync.
    /// </summary>
    protected ILogger EndpointLogger { get; private set; } = null!;

    /// <inheritdoc />
    // Why: IAuthorizationProvider.GetAllRoles() returns every role, including the seeded platform roles
    // (Admin, Operator, Viewer). Reading roles through IServiceConfigurationProvider<RoleConfiguration>
    // instead left those invisible.
    protected ListRolesEndpointBase(IAuthorizationProvider authorizationProvider)
    {
        _authorizationProvider = authorizationProvider;
    }

    /// <summary>
    /// Gets the RBAC policy required by this endpoint. Defaults to "settings/role:read".
    /// </summary>
    protected virtual string ReadPolicy => "settings/role:read";

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/roles");
        Policies(ReadPolicy);
        ConfigureEndpoint();
    }

    /// <summary>
    /// Override to configure endpoint-specific settings (auth, summary, etc.).
    /// </summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken ct)
    {
        EndpointLogger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        var allRoles = await _authorizationProvider.GetAllRoles(ct).ConfigureAwait(false);
        var roles = allRoles
            .Select(MapToSummary)
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.Name, StringComparer.Ordinal)
            .ToList();

        // Why: Newman/clients expect a paginated envelope {items, skip, take, totalCount, hasMore}
        // instead of a bare array; matches the response shape from /pipelines and other Crud-list endpoints.
        var response = PaginatedResponse<RoleSummaryResponse>.Create(roles, 0, roles.Count, roles.Count);
        await Send.OkAsync(response, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Maps a RoleConfiguration to a summary DTO. Override for custom mapping.
    /// </summary>
    protected virtual RoleSummaryResponse MapToSummary(RoleConfiguration role)
    {
        return new RoleSummaryResponse
        {
            Id = role.Id,
            Name = role.Name,
            DisplayName = role.DisplayName,
            Description = role.Description,
            IsTenantScoped = role.IsTenantScoped,
            SortOrder = role.SortOrder,
            CreatedAt = role.CreateDate
        };
    }
}
