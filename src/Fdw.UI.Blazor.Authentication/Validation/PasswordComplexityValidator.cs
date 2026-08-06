namespace Fdw.UI.Blazor.Authentication.Validation;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>
/// Validates password strings against configured complexity rules.
/// </summary>
public static class PasswordComplexityValidator
{
    /// <summary>
    /// Validates the specified password against the given complexity rules.
    /// </summary>
    /// <param name="password">The password to validate.</param>
    /// <param name="rules">The complexity rules to check against.</param>
    /// <returns>A <see cref="PasswordValidationResult"/> with the results.</returns>
    public static PasswordValidationResult Validate(string? password, PasswordComplexityRules rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        var errors = new List<string>();
        var value = password ?? string.Empty;

        bool meetsMinLength = value.Length >= rules.MinLength;
        if (!meetsMinLength)
        {
            errors.Add(string.Format(
                CultureInfo.InvariantCulture,
                "Password must be at least {0} characters long.",
                rules.MinLength));
        }

        bool hasUppercase = !rules.RequireUppercase || value.Any(char.IsUpper);
        if (!hasUppercase)
        {
            errors.Add("Password must contain at least one uppercase letter.");
        }

        bool hasLowercase = !rules.RequireLowercase || value.Any(char.IsLower);
        if (!hasLowercase)
        {
            errors.Add("Password must contain at least one lowercase letter.");
        }

        bool hasDigit = !rules.RequireDigit || value.Any(char.IsDigit);
        if (!hasDigit)
        {
            errors.Add("Password must contain at least one digit.");
        }

        bool hasSpecialCharacter = !rules.RequireSpecialCharacter || value.Any(c => !char.IsLetterOrDigit(c));
        if (!hasSpecialCharacter)
        {
            errors.Add("Password must contain at least one special character.");
        }

        bool isValid = meetsMinLength && hasUppercase && hasLowercase && hasDigit && hasSpecialCharacter;

        return new PasswordValidationResult(
            isValid,
            errors,
            meetsMinLength,
            hasUppercase,
            hasLowercase,
            hasDigit,
            hasSpecialCharacter);
    }
}
