using System;

namespace Fdw.Services.Authorization.Clients.Models;

/// <summary>
/// Represents a summary of a role's information.
/// </summary>
public sealed class RoleSummaryPayload
{
    /// <summary>
    /// Gets or sets the unique identifier for the role.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the unique name of the role.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Gets or sets the display name of the role.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the description of the role.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the role is scoped to a specific tenant.
    /// </summary>
    public bool IsTenantScoped { get; set; }

    /// <summary>
    /// Gets or sets the sort order for displaying the role.
    /// </summary>
    public int SortOrder { get; set; }
}
