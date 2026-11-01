using System;
using System.Collections.Generic;

namespace Fdw.Services.Authorization.Clients.Models;

/// <summary>
/// Represents detailed information about a role, including its permissions.
/// </summary>
public sealed class RoleDetailPayload
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
    /// Gets or sets the list of permissions associated with the role.
    /// </summary>
    public IReadOnlyList<PermissionPayload> Permissions { get; set; } = Array.Empty<PermissionPayload>();
}
