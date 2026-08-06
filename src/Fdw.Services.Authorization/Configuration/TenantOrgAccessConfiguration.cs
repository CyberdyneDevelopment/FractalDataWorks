using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Authorization.Configuration;

/// <summary>
/// Represents a row from <c>tenant.TenantOrgAccess</c>.
/// Each row grants either a role or a direct permission to a user within a specific
/// tenant-org combination. The authorization service unions these grants into the
/// effective permission set for the current-org tier.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class TenantOrgAccessConfiguration
{
    /// <summary>
    /// Gets or sets the user identifier (matches the <c>sub</c> claim in the JWT).
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier this grant belongs to.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the org identifier this grant belongs to.
    /// </summary>
    public Guid OrgId { get; set; }

    /// <summary>
    /// Gets or sets the role name granted to the user in this org, if any.
    /// Null when the grant is a direct permission (not role-based).
    /// </summary>
    public string? RoleName { get; set; }

    /// <summary>
    /// Gets or sets the bare permission name granted directly to the user in this org, if any.
    /// Null when the grant is role-based.
    /// </summary>
    public string? PermissionName { get; set; }
}
