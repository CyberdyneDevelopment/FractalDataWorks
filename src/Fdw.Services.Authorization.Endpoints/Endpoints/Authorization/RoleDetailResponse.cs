using System;
using System.Collections.Generic;
using Fdw.Web.Endpoints.Contracts;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>
/// Detailed response for a role, including permissions.
/// </summary>
public class RoleDetailResponse : ResourceDetail
{
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether this role is tenant-scoped.
    /// </summary>
    public bool IsTenantScoped { get; set; }

    /// <summary>
    /// Gets or sets the parent role name, if any.
    /// </summary>
    public string? ParentRoleName { get; set; }

    /// <summary>
    /// Gets or sets the sort order.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Gets or sets the permissions assigned to this role.
    /// </summary>
    public IList<PermissionSummaryDto> Permissions { get; set; } = [];

    /// <summary>
    /// Gets or sets when the role was created.
    /// </summary>
    public DateTimeOffset? CreatedAt { get; set; }
}
