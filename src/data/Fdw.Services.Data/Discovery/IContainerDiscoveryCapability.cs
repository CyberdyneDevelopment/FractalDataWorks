namespace Fdw.Services.Data.Discovery;

/// <summary>
/// Interface for data store types or configurations that support container discovery.
/// Implementers indicate whether automatic schema discovery is available
/// and provide the discovery method to use.
/// </summary>
public interface IContainerDiscoveryCapability
{
    /// <summary>
    /// Gets a value indicating whether this data store supports automatic container discovery
    /// via schema discovery.
    /// </summary>
    bool SupportsAutoDiscovery { get; }

    /// <summary>
    /// Gets the discovery method configured for this data store.
    /// Returns the prototype from the TypeCollection; call CreateInstance() to get a bindable copy.
    /// </summary>
    IDiscoveryMethod DiscoveryMethod { get; }
}
