namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Result of a DataStore setup operation (connection test + schema discovery + persistence).
/// </summary>
public sealed class SetupDataStoreResult
{
    /// <summary>Gets or sets the discovery ID (for SignalR progress subscriptions).</summary>
    public string DiscoveryId { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the connection test passed.</summary>
    public bool ConnectionTestPassed { get; set; }

    /// <summary>Gets or sets the name of the DataStore that was created, or <c>null</c> on failure.</summary>
    public string? DataStoreName { get; set; }
}
