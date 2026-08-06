namespace Fdw.Services.Connections.Clients.Models;

/// <summary>
/// Response DTO from DDL execution on a connection.
/// </summary>
// Why: pure response DTO, auto-properties only, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class ExecuteDdlConnectionResponse
{
    /// <summary>Gets or sets whether the execution was successful.</summary>
    public bool Success { get; set; }

    /// <summary>Gets or sets the result or error message.</summary>
    public string? Message { get; set; }
}
