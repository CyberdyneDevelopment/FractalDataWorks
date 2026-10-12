namespace Fdw.Schema.Endpoints;

/// <summary>
/// Validation warning for a mapping.
/// </summary>
public class MappingValidationWarning
{
    /// <summary>
    /// Gets or sets the warning code.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the warning message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}