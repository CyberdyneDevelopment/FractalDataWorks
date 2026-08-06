namespace Fdw.Orchestration.Pipelines.Abstractions;

/// <summary>
/// Represents a validation warning.
/// </summary>
// Why: pure result/warning POCO, auto-properties only, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class ValidationWarning
{
    /// <summary>
    /// Gets or sets the rule ID.
    /// </summary>
    public required string RuleId { get; set; }

    /// <summary>
    /// Gets or sets the field.
    /// </summary>
    public required string Field { get; set; }

    /// <summary>
    /// Gets or sets the warning message.
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Gets or sets the actual value.
    /// </summary>
    public object? ActualValue { get; set; }
}