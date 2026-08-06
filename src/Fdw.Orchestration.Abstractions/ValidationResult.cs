using System.Collections.Generic;

namespace Fdw.Orchestration.Pipelines.Abstractions;

/// <summary>
/// Represents the result of validating a record.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Gets or sets whether the validation passed.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the validation errors.
    /// </summary>
    public IList<ValidationError> Errors { get; set; } = [];

    /// <summary>
    /// Gets or sets the validation warnings.
    /// </summary>
    public IList<ValidationWarning> Warnings { get; set; } = [];
}