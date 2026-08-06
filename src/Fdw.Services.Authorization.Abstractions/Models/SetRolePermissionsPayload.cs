using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Fdw.Services.Authorization.Clients.Models;

/// <summary>
/// Data transfer object for setting permissions on a role.
/// </summary>
public sealed class SetRolePermissionsPayload
{
    /// <summary>
    /// Gets or sets the list of permission names to assign to the role.
    /// </summary>
    // Why: the server endpoint binds the JSON body's "permissions" property
    // ([JsonPropertyName("permissions")]). Without this matching name the PUT serializes
    // "permissionNames", the server resolves zero permissions, and the role is silently wiped.
    [Required]
    [JsonPropertyName("permissions")]
    public IReadOnlyList<string> PermissionNames { get; set; } = Array.Empty<string>();
}
