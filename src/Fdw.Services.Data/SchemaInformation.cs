using System;
using System.Collections.Generic;
using Fdw.Services.Connections;

namespace Fdw.Services.Data;

/// <summary>
/// The discovered schema for a connection: the DataStore and its full hierarchy of
/// DataPaths (schemas) → DataContainers (tables/views) → DataContainerFields (columns).
/// </summary>
/// <remarks>
/// This is the read model returned by <see cref="ISchemaInformationService"/>.
/// It wraps the persisted <see cref="DataStoreConfiguration"/> and its nested collections
/// so callers receive one coherent snapshot without needing to query multiple providers.
/// </remarks>
public sealed class SchemaInformation
{
    /// <summary>
    /// Gets the DataStore configuration that this schema belongs to.
    /// </summary>
    public DataStoreConfiguration DataStore { get; }

    /// <summary>
    /// Gets the DataPaths (schemas) within the DataStore, each containing their containers and fields.
    /// </summary>
    public IReadOnlyList<DataPathConfiguration> Paths { get; }

    /// <summary>
    /// Gets the connection ID that this DataStore was discovered from.
    /// </summary>
    public Guid ConnectionId => DataStore.ConnectionId;

    /// <summary>
    /// Gets the timestamp of the last successful schema discovery.
    /// </summary>
    public DateTimeOffset? LastDiscoveredAt => DataStore.LastDiscoveredAt;

    /// <summary>
    /// Initializes a new instance of <see cref="SchemaInformation"/>.
    /// </summary>
    /// <param name="dataStore">The DataStore configuration including nested paths and containers.</param>
    public SchemaInformation(DataStoreConfiguration dataStore)
    {
        DataStore = dataStore;
        Paths = dataStore.Paths;
    }
}
