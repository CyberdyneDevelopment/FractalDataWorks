namespace Fdw.Data.Components.Models;

/// <summary>
/// A validation warning for a field mapping.
/// </summary>
public sealed class ValidationWarningDto
{
    /// <summary>Gets or sets the warning code.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Gets or sets the warning message.</summary>
    public string Message { get; set; } = string.Empty;
}
