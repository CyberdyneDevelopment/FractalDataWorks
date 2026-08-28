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
    [Required]
    [JsonPropertyName("permissions")]
    public IList<string> PermissionNames { get; set; } = [];
}
