using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Connections;

namespace Fdw.Data.SchemaImporters.Abstractions;

/// <summary>
/// Persists discovered schema to the configuration database.
/// Maps the discovered <see cref="DataStoreConfiguration"/> hierarchy
/// (DataStore/DataPath/DataContainer/DataContainerField) to ManagedConfiguration tables.
/// </summary>
public interface ISchemaImportPersister
{
    /// <summary>
    /// Persists a discovered DataStore configuration and all its paths/containers/fields to the configuration database.
    /// </summary>
    /// <param name="discovered">The discovered DataStore configuration from schema import.</param>
    /// <param name="connectionId">The Connection ID this DataStore is accessed through.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the persisted DataStore configuration ID.</returns>
    Task<IGenericResult<Guid>> Persist(
        DataStoreConfiguration discovered,
        Guid connectionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Syncs an existing DataStore configuration with newly discovered schema.
    /// Detects added, modified, and removed objects.
    /// </summary>
    /// <param name="existingDataStoreId">The existing DataStore configuration ID.</param>
    /// <param name="discovered">The newly discovered DataStore configuration from schema import.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing sync statistics.</returns>
    Task<IGenericResult<SchemaImportSyncResult>> Sync(
        Guid existingDataStoreId,
        DataStoreConfiguration discovered,
        CancellationToken cancellationToken = default);
}
