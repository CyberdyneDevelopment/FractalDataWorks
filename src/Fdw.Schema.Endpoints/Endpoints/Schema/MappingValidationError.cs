namespace Fdw.Schema.Endpoints;

/// <summary>
/// Validation error for a mapping.
/// </summary>
public class MappingValidationError
{
    /// <summary>
    /// Gets or sets the error code.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the property path.
    /// </summary>
    public string? PropertyPath { get; set; }
}