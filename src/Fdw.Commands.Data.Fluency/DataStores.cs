namespace Fdw.Commands.Data;

/// <summary>
/// Static entry point for hierarchical data store access.
/// Provides discoverable, semantic query construction with clear DataStore, Path, and Container selection.
/// </summary>
public static class DataStores
{
    /// <summary>
    /// Select a data store to query from (required).
    /// </summary>
    /// <param name="dataStoreName">The data store name (e.g., "AuthDb", "ConfigurationDb", "CRM").</param>
    /// <returns>A builder for selecting paths within this data store.</returns>
    /// <example>
    /// <code>
    /// // Query from AuthDb
    /// var command = DataStores.For("AuthDb")
    ///     .Path("auth")
    ///     .Container&lt;User&gt;("Users")
    ///     .Where("Username", username)
    ///     .Build();
    ///
    /// // Query from ConfigurationDb
    /// var command = DataStores.For("ConfigurationDb")
    ///     .Path("cfg")
    ///     .Container&lt;Connection&gt;("Connections")
    ///     .Where("IsActive", true)
    ///     .Build();
    ///
    /// var result = await dataGateway.Execute(command, ct);
    /// </code>
    /// </example>
    public static DataStoreBuilder For(string dataStoreName)
    {
        return new DataStoreBuilder(dataStoreName);
    }
}