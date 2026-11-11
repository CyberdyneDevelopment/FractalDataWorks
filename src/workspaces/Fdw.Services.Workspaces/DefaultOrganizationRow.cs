using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Workspaces;

/// <summary>
/// The columns of <c>tenant.Organization</c> the placement of a new member needs to find a tenant's default workspace (FDW-3692).
/// </summary>
/// <remarks>
/// Read by <see cref="DefaultWorkspacePlacement"/> from the organization domain table itself, on whatever host makes the member: the
/// organization provider a host registers depends on its multitenancy option (a single-tenant host registers one that knows no
/// organization), and a placement that depended on it would refuse every create on those hosts.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class DefaultOrganizationRow : IGenericConfiguration
{
    /// <summary>Gets or sets the organization's durable Id.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the organization's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the Id of the workspace that is the organization's default; null when it has none.</summary>
    public Guid? DefaultWorkspaceId { get; set; }
}
