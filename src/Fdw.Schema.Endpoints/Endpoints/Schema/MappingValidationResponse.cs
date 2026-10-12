using System.Collections.Generic;

namespace Fdw.Schema.Endpoints;

/// <summary>
/// Response for validation endpoint.
/// </summary>
public class MappingValidationResponse
{
    /// <summary>
    /// Gets or sets whether the mappings are valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the validation errors.
    /// </summary>
    public IList<MappingValidationError> Errors { get; set; } = [];

    /// <summary>
    /// Gets or sets the validation warnings.
    /// </summary>
    public IList<MappingValidationWarning> Warnings { get; set; } = [];
}