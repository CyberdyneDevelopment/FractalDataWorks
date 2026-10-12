using System.ComponentModel.DataAnnotations;

namespace Fdw.Services.Authorization.Clients.Models;

/// <summary>
/// Data transfer object for assigning a role to a user. The role is granted in the tenant the
/// caller's token is acting in; there is no tenant to send.
/// </summary>
public sealed class AssignRolePayload
{
    /// <summary>
    /// Gets or sets the role name to assign.
    /// </summary>
    [Required]
    public string RoleName { get; set; } = "";
}
