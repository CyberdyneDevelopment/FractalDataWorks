using Fdw.Configuration;
using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Registry of data store types. Pure type collection with no DI orchestration.
/// DI registration is handled by DataStoreProvider.
/// </summary>
/// <remarks>
/// Uses [MutableTypeCollection] to support cross-assembly TypeOption registration
/// (e.g., MsSqlDataStoreType in Services.Connections.MsSql).
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(
    typeof(DataStoreTypeBase<IGenericConfiguration>),
    typeof(IDataStoreType),
    typeof(DataStoreTypes))]
public abstract partial class DataStoreTypes : TypeCollectionBase<
    DataStoreTypeBase<IGenericConfiguration>,
    IDataStoreType>
{
    /// <summary>
    /// The service category for database configuration loading.
    /// </summary>
    public static string ServiceCategory => "DataStore";

    /// <summary>
    /// The connection the datastore and dataset configuration rows are read from and written to.
    /// </summary>
    /// <remarks>
    /// The host's <c>ConnectionRoles.Platform</c> declaration: a host can keep these rows in a store of its own
    /// naming, and the choice cannot be fixed when the container is built.
    /// </remarks>
    public static string ConfigurationConnection => PlatformConnection.Name;
}
