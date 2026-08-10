using System;
using Fdw.Web.Endpoints.Contracts;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>
/// Summary response for a role.
/// </summary>
public class RoleSummaryResponse : ResourceSummary
{
    /// <summary>
    /// Gets or sets the role ID.
    /// </summary>
    public Guid Id { get; set; }

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
    /// Gets or sets the sort order.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Gets or sets when the role was created.
    /// </summary>
    public DateTimeOffset? CreatedAt { get; set; }
}
