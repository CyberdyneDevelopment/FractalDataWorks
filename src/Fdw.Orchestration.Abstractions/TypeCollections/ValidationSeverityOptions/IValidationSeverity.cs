using Fdw.Collections;

namespace Fdw.Orchestration.Abstractions.TypeCollections.ValidationSeverityOptions;

/// <summary>
/// Interface for validation severity TypeOptions.
/// </summary>
/// <remarks>
/// Validation severities classify the importance of validation results,
/// from informational messages to critical errors that block execution.
/// </remarks>
public interface IValidationSeverity : ITypeOption<int, ValidationSeverityBase>
{
    /// <summary>
    /// Gets the severity level for ordering (higher = more severe).
    /// </summary>
    int Level { get; }

    /// <summary>
    /// Gets whether this severity level blocks execution.
    /// </summary>
    bool BlocksExecution { get; }

    /// <summary>
    /// Gets whether this severity level requires user acknowledgment to proceed.
    /// </summary>
    bool RequiresAcknowledgment { get; }

    /// <summary>
    /// Gets whether this severity level should be logged.
    /// </summary>
    bool ShouldLog { get; }
}
