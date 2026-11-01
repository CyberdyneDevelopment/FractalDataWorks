namespace Fdw.Schema.Clients.Models;

/// <summary>
/// Response from a DDL execution request.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class ExecuteDdlResponse
{
    /// <summary>Gets or sets a value indicating whether the execution was successful.</summary>
    public bool Success { get; set; }
    /// <summary>Gets or sets the result or error message.</summary>
    public string? Message { get; set; }
}
