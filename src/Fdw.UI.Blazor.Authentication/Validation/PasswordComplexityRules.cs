namespace Fdw.UI.Blazor.Authentication.Validation;

/// <summary>
/// Configures password complexity requirements for validation.
/// </summary>
public sealed class PasswordComplexityRules
{
    /// <summary>
    /// Gets or sets the minimum password length. Defaults to 8.
    /// </summary>
    public int MinLength { get; set; } = 8;

    /// <summary>
    /// Gets or sets a value indicating whether an uppercase letter is required. Defaults to <c>true</c>.
    /// </summary>
    public bool RequireUppercase { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether a lowercase letter is required. Defaults to <c>true</c>.
    /// </summary>
    public bool RequireLowercase { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether a digit is required. Defaults to <c>true</c>.
    /// </summary>
    public bool RequireDigit { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether a special character is required. Defaults to <c>true</c>.
    /// </summary>
    public bool RequireSpecialCharacter { get; set; } = true;
}
