using System;
using System.Collections.Generic;
using Fdw.Orchestration.Abstractions.TypeCollections.ValidationSeverityOptions;

namespace Fdw.Orchestration.Pipelines.Abstractions.TypeCollections.ValidationRuleTypeOptions;

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
public sealed class ValidationResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationResult"/> class.
    /// </summary>
    /// <param name="isValid">Whether validation passed.</param>
    /// <param name="message">Validation message.</param>
    /// <param name="severity">The severity of any validation issues.</param>
    /// <param name="fieldErrors">Field-specific errors.</param>
    public ValidationResult(
        bool isValid,
        string? message = null,
        IValidationSeverity? severity = null,
        IReadOnlyDictionary<string, string>? fieldErrors = null)
    {
        IsValid = isValid;
        Message = message;
        Severity = severity;
        FieldErrors = fieldErrors ?? new Dictionary<string, string>(StringComparer.Ordinal);
    }

    /// <summary>
    /// Gets whether the validation passed.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Gets the validation message.
    /// </summary>
    public string? Message { get; }

    /// <summary>
    /// Gets the severity of validation issues (if any).
    /// </summary>
    public IValidationSeverity? Severity { get; }

    /// <summary>
    /// Gets field-specific error messages.
    /// </summary>
    public IReadOnlyDictionary<string, string> FieldErrors { get; }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <returns>A valid validation result.</returns>
    public static ValidationResult Success() => new(true);

    /// <summary>
    /// Creates a successful validation result with a message.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <returns>A valid validation result.</returns>
    public static ValidationResult Success(string message) => new(true, message);

    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="severity">The severity level.</param>
    /// <returns>An invalid validation result.</returns>
    public static ValidationResult Failure(string message, IValidationSeverity? severity = null) =>
        new(false, message, severity);

    /// <summary>
    /// Creates a failed validation result with field-specific errors.
    /// </summary>
    /// <param name="message">The overall error message.</param>
    /// <param name="fieldErrors">Field-specific errors.</param>
    /// <param name="severity">The severity level.</param>
    /// <returns>An invalid validation result.</returns>
    public static ValidationResult Failure(
        string message,
        IReadOnlyDictionary<string, string> fieldErrors,
        IValidationSeverity? severity = null) =>
        new(false, message, severity, fieldErrors);
}
