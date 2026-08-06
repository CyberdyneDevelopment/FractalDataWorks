namespace Fdw.Services.Authentication.Clients.Models;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Form-facing request model for user login with validation attributes for Blazor form binding.
/// </summary>
public sealed class LoginRequest
{
    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    [Required(ErrorMessage = "Username is required")]
    public string Username { get; set; } = "";

    /// <summary>
    /// Gets or sets the password.
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = "";

    /// <summary>
    /// Gets or sets the optional tenant identifier or slug for multi-tenant login.
    /// </summary>
    public string? Tenant { get; set; }
}
