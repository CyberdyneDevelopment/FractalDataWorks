using Fdw.Orchestration.Abstractions.TypeCollections.ValidationSeverityOptions;

namespace Fdw.Orchestration.Pipelines.Abstractions;

/// <summary>
/// Represents a validation error.
/// </summary>
// Why: pure result/warning POCO, auto-properties only, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class ValidationError
{
    /// <summary>
    /// Gets or sets the rule ID that failed.
    /// </summary>
    public required string RuleId { get; set; }

    /// <summary>
    /// Gets or sets the field that failed validation.
    /// </summary>
    public required string Field { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Gets or sets the actual value that failed.
    /// </summary>
    public object? ActualValue { get; set; }

    /// <summary>
    /// Gets or sets the severity.
    /// </summary>
    public required IValidationSeverity Severity { get; set; }
}