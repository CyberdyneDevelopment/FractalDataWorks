using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>
/// Request to set permissions for a role.
/// </summary>
public class SetRolePermissionsRequest
{
    /// <summary>
    /// Gets or sets the role name (bound from route).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the permission names to assign, replacing the role's current set. Bound from the
    /// JSON body's <c>permissions</c> property. Omitted (null) leaves the role's permissions
    /// untouched; an empty list is a real request to strip every permission from the role.
    /// </summary>
    [JsonPropertyName("permissions")]
    public IList<string>? PermissionNames { get; set; }
}
