namespace Fdw.Services.Connections.Clients.Models;

/// <summary>
/// Summary of connection setup initiated after creation.
/// </summary>
public sealed class ConnectionSetupSummaryPayload
{
    /// <summary>
    /// Gets or sets the discovery ID for subscribing to SignalR progress updates.
    /// </summary>
    public string DiscoveryId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the connection test passed.
    /// </summary>
    public bool ConnectionTestPassed { get; set; }

    /// <summary>
    /// Gets or sets the name of the DataStore that was created, if any.
    /// </summary>
    public string? DataStoreName { get; set; }
}
