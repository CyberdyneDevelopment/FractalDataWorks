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
    /// Gets or sets the sign-in methods to create with the account. At least one is required.
    /// </summary>
    /// <remarks>
    /// An account may sign in with a password, through an external identity provider, with an API
    /// key, or with any combination of the three. The methods are created in one request because a
    /// user with no way in is not a user anyone asked for.
    /// </remarks>
    public SignInMethodsRequest SignInMethods { get; set; } = new();

    /// <summary>
    /// Gets or sets the email address for the new user.
    /// </summary>
    [EmailAddress]
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the roles the new user holds in its tenant. Required: an empty list states "no
    /// roles", and an absent one is refused.
    /// </summary>
    /// <remarks>
    /// It used to default to <c>User</c>, a role no seed creates, so a request that left it out failed
    /// half-way through creating the account (FDW-910). Which roles an account starts with is the
    /// caller's decision; there is no role every deployment has.
    /// </remarks>
    public IList<string>? Roles { get; set; }
}
