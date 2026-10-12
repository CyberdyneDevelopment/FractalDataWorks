using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Data;

/// <summary>
/// Discovers the schema of a connection, and adds what it finds to data stores. Every caller that
/// discovers goes through here; none resolves a connection type's discovery itself.
/// </summary>
/// <remarks>
/// <para>
/// This service is the primary entry point for getting DataStore metadata at runtime.
/// It replaces the startup-only <c>SchemaDiscoveryStartupService</c> pattern with a
/// demand-driven API that endpoints and background services can call directly.
/// </para>
/// <para>
/// <see cref="GetSchema"/> returns the connection's stored data store when it has one; otherwise it
/// discovers, persists, and returns it. <see cref="RefreshSchema"/> always re-discovers, enabling the
/// "Re-discover" button in the Management UI. <see cref="DiscoverContainers"/> discovers without
/// saving; <see cref="DiscoverInto"/> adds what discovery finds to a given store without saving it.
/// </para>
/// <para>
/// Whether and how a connection discovers is its chosen option,
/// <see cref="Fdw.Services.Connections.Abstractions.IConnectionImplementationConfiguration.SchemaDiscovery"/>,
/// which its connection type must offer. A connection whose option is <c>None</c> is refused, never
/// reported as an empty store. Discovery *scope* (which db/schema/table is visible) is expressed by
/// DataStore/DataPath/DataContainer records and gated by RBAC; Connection carries no schema
/// include/exclude lists.
/// </para>
/// </remarks>
public interface ISchemaInformationService
{
    /// <summary>
    /// Gets schema information for the named connection.
    /// Returns the stored DataStore when the connection has one; discovers and persists it when not.
    /// </summary>
    /// <param name="connectionName">The name of the connection to get schema for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A success result containing <see cref="SchemaInformation"/>, or a failure result if the
    /// connection cannot be resolved or, having no store, cannot discover.
    /// </returns>
    Task<IGenericResult<SchemaInformation>> GetSchema(
        string connectionName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Forces re-discovery of schema for the named connection, persists the results, and returns updated metadata.
    /// </summary>
    /// <param name="connectionName">The name of the connection to re-discover.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A success result containing <see cref="SchemaInformation"/>, or a failure result if the
    /// connection cannot be resolved or cannot discover.
    /// </returns>
    Task<IGenericResult<SchemaInformation>> RefreshSchema(
        string connectionName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers what the named connection's source holds, using the connection's chosen option.
    /// Nothing is saved.
    /// </summary>
    /// <param name="connectionName">The name of the connection to discover through.</param>
    /// <param name="options">Which schemas and object kinds to include.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The discovered containers, or a failure saying why the connection cannot discover.</returns>
    Task<IGenericResult<IReadOnlyList<IStorageContainer>>> DiscoverContainers(
        string connectionName,
        DataStoreDiscoveryOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers through the store's connection and adds to the store the paths, containers, fields
    /// and keys it lacks. The store is changed in place and not saved: the caller saves it once, with
    /// whatever else the same request changed.
    /// </summary>
    /// <param name="store">The store; changed in place.</param>
    /// <param name="options">Which schemas and object kinds to include.</param>
    /// <param name="refresh">Whether containers the store already has also gain the fields they lack.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>What was found and added, or a failure saying why the connection cannot discover.</returns>
    Task<IGenericResult<DataStoreSchemaImportResult>> DiscoverInto(
        IDataStoreImplementationConfiguration store,
        DataStoreDiscoveryOptions options,
        bool refresh,
        CancellationToken cancellationToken = default);
}
