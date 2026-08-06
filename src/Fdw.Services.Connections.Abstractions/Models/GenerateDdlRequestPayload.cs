namespace Fdw.Services.Connections.Clients.Models;

/// <summary>
/// Request payload for generating DDL from a connection's schema.
/// </summary>
// Why: pure request payload, auto-properties only, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class GenerateDdlRequestPayload
{
    /// <summary>Gets or sets optional schema filter.</summary>
    public string? SchemaFilter { get; set; }
}
