using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Fdw.Services.Users.Clients.Models;

/// <summary>
/// Data transfer object for creating a new user.
/// </summary>
public class CreateUserRequest
{
    /// <summary>
    /// Gets or sets the username for the new user.
    /// </summary>
    [Required, StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = "";

    /// <summary>
    /// Gets or sets the password for the new user.
    /// </summary>
    [Required, MinLength(8)]
    public string Password { get; set; } = "";

    /// <summary>
    /// Gets or sets the email address for the new user.
    /// </summary>
    [EmailAddress]
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the initial roles to assign to the new user.
    /// </summary>
    public IList<string> Roles { get; set; } = new List<string> { "User" };
}
