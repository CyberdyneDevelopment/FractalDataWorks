using System.Threading.Tasks;

namespace Fdw.Services.Data.SignalR;

/// <summary>
/// Client-side SignalR hub interface for schema discovery progress notifications.
/// </summary>
public interface ISchemaDiscoveryHubClient
{
    /// <summary>Notifies that schema discovery has started.</summary>
    Task DiscoveryStarted(SchemaDiscoveryStartedEvent evt);

    /// <summary>Notifies schema discovery progress updates.</summary>
    Task DiscoveryProgress(SchemaDiscoveryProgressEvent evt);

    /// <summary>Notifies that a table or view was discovered.</summary>
    Task ObjectDiscovered(SchemaObjectDiscoveredEvent evt);

    /// <summary>Notifies that schema discovery completed successfully.</summary>
    Task DiscoveryCompleted(SchemaDiscoveryCompletedEvent evt);

    /// <summary>Notifies that schema discovery failed.</summary>
    Task DiscoveryFailed(SchemaDiscoveryFailedEvent evt);
}