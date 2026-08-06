using System;

namespace Fdw.Services.Authorization.Clients.Models;

/// <summary>
/// Represents a permission.
/// </summary>
public sealed class PermissionPayload
{
    /// <summary>
    /// Gets or sets the unique identifier for the permission.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the unique name of the permission.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Gets or sets the resource the permission applies to.
    /// </summary>
    public string Resource { get; set; } = "";

    /// <summary>
    /// Gets or sets the action allowed by the permission.
    /// </summary>
    public string Action { get; set; } = "";

    /// <summary>
    /// Gets or sets the display name of the permission.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the description of the permission.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the category the permission belongs to.
    /// </summary>
    public string? Category { get; set; }
}
