using System;
using System.Collections.Generic;

namespace Fdw.Services.Authorization.Clients.Models;

/// <summary>
/// Represents a group of permissions, typically organized by resource.
/// </summary>
public sealed class PermissionGroupPayload
{
    /// <summary>
    /// Gets or sets the name of the resource for this group of permissions.
    /// </summary>
    public string Resource { get; set; } = "";

    /// <summary>
    /// Gets or sets the list of permissions in this group.
    /// </summary>
    public IReadOnlyList<PermissionPayload> Permissions { get; set; } = Array.Empty<PermissionPayload>();
}
