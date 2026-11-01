namespace Fdw.Services.Users.Clients.Models;

/// <summary>
/// The password sign-in method to create with a new user.
/// </summary>
/// <remarks>
/// The password given here is the account's first password and is always marked must-change, so the
/// person who receives it chooses their own before the account is usable.
/// </remarks>
public class PasswordSignInMethod
{
    /// <summary>
    /// Gets or sets the initial password. Subject to the same policy as any other password.
    /// </summary>
    public string InitialPassword { get; set; } = string.Empty;
}
