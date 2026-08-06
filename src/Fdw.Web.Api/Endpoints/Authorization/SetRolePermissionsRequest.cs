using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
    /// Gets or sets the permission names to assign. Bound from the JSON body's
    /// <c>permissions</c> property.
    /// </summary>
    // Why: the client contract marks this Required; without the matching server-side check a
    // request omitting "permissions" was accepted and silently set the role to zero permissions.
    [Required]
    [JsonPropertyName("permissions")]
    public IList<string> PermissionNames { get; set; } = [];
}
