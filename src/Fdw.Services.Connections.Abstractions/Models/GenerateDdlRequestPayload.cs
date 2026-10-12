namespace Fdw.Services.Connections.Clients.Models;

/// <summary>
/// Request payload for generating DDL from a connection's schema.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class GenerateDdlRequestPayload
{
    /// <summary>Gets or sets optional schema filter.</summary>
    public string? SchemaFilter { get; set; }
}
