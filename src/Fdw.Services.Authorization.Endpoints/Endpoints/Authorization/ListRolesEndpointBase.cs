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
    /// <summary>Initializes a new instance of the <see cref="ListRolesEndpointBase"/> class.</summary>
        private readonly IRoleConfigurationProvider _roleProvider;

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="ListRolesEndpointBase"/> class.</summary>
    protected ListRolesEndpointBase(ILogger logger, IRoleConfigurationProvider roleProvider)
    {
        EndpointLogger = logger;
        _roleProvider = roleProvider;
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

        var allRolesResult = await _roleProvider.Get(ct).ConfigureAwait(false);
        if (!allRolesResult.IsSuccess || allRolesResult.Value is null)
        {
            AuthorizationEndpointLog.AuthorizationReadFailed(
                EndpointLogger, "roles", allRolesResult.CurrentMessage);
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }
        var allRoles = allRolesResult.Value;
        var roles = allRoles
            .Select(MapToSummary)
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.Name, StringComparer.Ordinal)
            .ToList();

        var response = PaginatedResponse<RoleSummaryResponse>.Create(roles, 0, roles.Count, roles.Count);
        await Send.OkAsync(response, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Maps a role implementation configuration to a summary DTO. Override for custom mapping.
    /// </summary>
    protected virtual RoleSummaryResponse MapToSummary(IRoleImplementationConfiguration role)
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
