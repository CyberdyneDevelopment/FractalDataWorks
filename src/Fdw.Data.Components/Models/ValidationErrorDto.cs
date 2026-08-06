namespace Fdw.Data.Components.Models;

/// <summary>
/// A validation error for a field mapping.
/// </summary>
public sealed class ValidationErrorDto
{
    /// <summary>Gets or sets the error code.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Gets or sets the error message.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Gets or sets the property path that has the error.</summary>
    public string? PropertyPath { get; set; }
}
