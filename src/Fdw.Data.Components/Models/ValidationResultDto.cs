using System.Collections.Generic;

namespace Fdw.Data.Components.Models;

/// <summary>
/// Result of validating field mappings.
/// </summary>
public sealed class ValidationResultDto
{
    /// <summary>Gets or sets whether all mappings are valid.</summary>
    public bool IsValid { get; set; }

    /// <summary>Gets or sets the validation errors.</summary>
    public IReadOnlyList<ValidationErrorDto> Errors { get; set; } = [];

    /// <summary>Gets or sets the validation warnings.</summary>
    public IReadOnlyList<ValidationWarningDto> Warnings { get; set; } = [];
}
