namespace Fdw.UI.Blazor.Authentication.Validation;

using System.Collections.Generic;

/// <summary>
/// Represents the result of password complexity validation.
/// </summary>
public sealed class PasswordValidationResult
{
    /// <summary>
    /// Gets a value indicating whether all password rules passed.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Gets the list of validation error messages.
    /// </summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>
    /// Gets a value indicating whether the minimum length requirement is met.
    /// </summary>
    public bool MeetsMinLength { get; }

    /// <summary>
    /// Gets a value indicating whether the uppercase requirement is met.
    /// </summary>
    public bool HasUppercase { get; }

    /// <summary>
    /// Gets a value indicating whether the lowercase requirement is met.
    /// </summary>
    public bool HasLowercase { get; }

    /// <summary>
    /// Gets a value indicating whether the digit requirement is met.
    /// </summary>
    public bool HasDigit { get; }

    /// <summary>
    /// Gets a value indicating whether the special character requirement is met.
    /// </summary>
    public bool HasSpecialCharacter { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PasswordValidationResult"/> class.
    /// </summary>
    /// <param name="isValid">Whether all rules passed.</param>
    /// <param name="errors">The list of error messages.</param>
    /// <param name="meetsMinLength">Whether the min length is met.</param>
    /// <param name="hasUppercase">Whether uppercase is present.</param>
    /// <param name="hasLowercase">Whether lowercase is present.</param>
    /// <param name="hasDigit">Whether a digit is present.</param>
    /// <param name="hasSpecialCharacter">Whether a special character is present.</param>
    public PasswordValidationResult(
        bool isValid,
        IReadOnlyList<string> errors,
        bool meetsMinLength,
        bool hasUppercase,
        bool hasLowercase,
        bool hasDigit,
        bool hasSpecialCharacter)
    {
        IsValid = isValid;
        Errors = errors;
        MeetsMinLength = meetsMinLength;
        HasUppercase = hasUppercase;
        HasLowercase = hasLowercase;
        HasDigit = hasDigit;
        HasSpecialCharacter = hasSpecialCharacter;
    }
}
