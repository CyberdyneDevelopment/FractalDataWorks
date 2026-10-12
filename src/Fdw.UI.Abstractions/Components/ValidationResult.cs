using System.Collections.Generic;
using System.Linq;

namespace Fdw.UI.Abstractions.Components;

/// <summary>
/// Represents the result of a component validation.
/// </summary>
public sealed class ValidationResult
{
    private readonly List<ValidationMessage> _messages;

    private ValidationResult(bool isValid, IEnumerable<ValidationMessage>? messages = null)
    {
        IsValid = isValid;
        _messages = messages?.ToList() ?? [];
    }

    /// <summary>
    /// Gets a value indicating whether the validation passed.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Gets the validation messages.
    /// </summary>
    public IReadOnlyList<ValidationMessage> Messages => _messages.AsReadOnly();

    /// <summary>
    /// Gets the first error message, if any.
    /// </summary>
    public string? FirstError => _messages
        .FirstOrDefault(m => string.Equals(m.Severity.Name, "Error", System.StringComparison.Ordinal))?.Message;

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <returns>A successful validation result.</returns>
    public static ValidationResult Success() => new(true);

    /// <summary>
    /// Creates a successful validation result with a warning.
    /// </summary>
    /// <param name="warning">The warning message.</param>
    /// <returns>A successful validation result with a warning.</returns>
    public static ValidationResult SuccessWithWarning(string warning)
        => new(true, [new ValidationMessage(warning, ValidationSeverities.Warning)]);

    /// <summary>
    /// Creates a failed validation result with an error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed validation result.</returns>
    public static ValidationResult Error(string error)
        => new(false, [new ValidationMessage(error, ValidationSeverities.Error)]);

    /// <summary>
    /// Creates a failed validation result with multiple errors.
    /// </summary>
    /// <param name="errors">The error messages.</param>
    /// <returns>A failed validation result.</returns>
    public static ValidationResult Errors(IEnumerable<string> errors)
        => new(false, errors.Select(e => new ValidationMessage(e, ValidationSeverities.Error)));

    /// <summary>
    /// Creates a validation result with the specified messages.
    /// </summary>
    /// <param name="messages">The validation messages.</param>
    /// <returns>A validation result.</returns>
    public static ValidationResult FromMessages(IEnumerable<ValidationMessage> messages)
    {
        var messageList = messages.ToList();
        var hasErrors = messageList.Any(m => string.Equals(m.Severity.Name, "Error", System.StringComparison.Ordinal));
        return new ValidationResult(!hasErrors, messageList);
    }

    /// <summary>
    /// Combines multiple validation results.
    /// </summary>
    /// <param name="results">The results to combine.</param>
    /// <returns>A combined validation result.</returns>
    public static ValidationResult Combine(params ValidationResult[] results)
    {
        var allMessages = results.SelectMany(r => r.Messages).ToList();
        var isValid = results.All(r => r.IsValid);
        return new ValidationResult(isValid, allMessages);
    }
}
