using System.ComponentModel.DataAnnotations;

namespace Fdw.Services.Users.Clients.Models;

/// <summary>
/// Data transfer object for assigning a role to a user.
/// </summary>
public sealed class AssignRoleRequest
{
    /// <summary>
    /// Gets or sets the name of the role to assign.
    /// </summary>
    [Required]
    public string RoleName { get; set; } = "";
}
