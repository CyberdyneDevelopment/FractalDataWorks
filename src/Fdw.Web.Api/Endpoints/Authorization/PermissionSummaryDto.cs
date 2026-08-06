using System;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>
/// Summary DTO for a permission.
/// </summary>
public class PermissionSummaryDto
{
    /// <summary>
    /// Gets or sets the permission ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the permission name (e.g., "connections:read").
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the service domain (e.g., "connections", "datastores").
    /// </summary>
    public required string Domain { get; set; }

    /// <summary>
    /// Gets or sets the specific resource within the domain (e.g., "mssql", "*").
    /// </summary>
    public required string Resource { get; set; }

    /// <summary>
    /// Gets or sets the action name (e.g., "read", "write").
    /// </summary>
    public required string Action { get; set; }

    /// <summary>
    /// Gets or sets the permission scope ("tenant", "system", "global").
    /// </summary>
    public required string Scope { get; set; }

    /// <summary>
    /// Gets or sets the category for grouping.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets the display sort order for grouping in the UI.
    /// </summary>
    public int SortOrder { get; set; }
}
