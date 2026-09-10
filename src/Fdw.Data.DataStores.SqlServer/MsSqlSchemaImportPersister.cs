using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Conventions;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Results.Abstractions;
using Fdw.Data.DataStores.SqlServer.Results;
using Fdw.Data.DataStores.SqlServer.Logging;
using Fdw.Data.SchemaImporters.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Connections.MsSql;
using Fdw.Services.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.DataStores.SqlServer;

/// <summary>
/// Persists discovered SQL Server schema (a <see cref="DataStoreImplementationConfiguration"/>) to ManagedConfiguration tables.
/// </summary>
/// <remarks>
/// The paths, containers and fields are the store's own children, so both writes build or amend the
/// store in memory and save it once, through the DataStore domain.
/// </remarks>
[ExcludeFromCodeCoverage] // Excluded: requires SQL Server connection
public sealed class MsSqlSchemaImportPersister : ISchemaImportPersister
{
    private readonly DataStoreConfigurationProvider _dataStoreProvider;
    private readonly ConnectionConfigurationProvider _connectionProvider;
    private readonly ILogger<MsSqlSchemaImportPersister> _logger;

    /// <summary>Initializes a new instance of the <see cref="MsSqlSchemaImportPersister"/> class.</summary>
    public MsSqlSchemaImportPersister(
        DataStoreConfigurationProvider dataStoreProvider,
        ConnectionConfigurationProvider connectionProvider,
        ILogger<MsSqlSchemaImportPersister>? logger)
    {
        _dataStoreProvider = dataStoreProvider ?? throw new ArgumentNullException(nameof(dataStoreProvider));
        _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
        _logger = logger ?? NullLogger<MsSqlSchemaImportPersister>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<Guid>> Persist(
        DataStoreImplementationConfiguration discovered,
        Guid connectionId,
        CancellationToken cancellationToken = default)
    {
        if (discovered is null)
        {
            return GenericResult<Guid>.Failure(SqlServerDataStoreResultCodes.ByName("DataStoreNull"));
        }

        if (connectionId == Guid.Empty)
        {
            return GenericResult<Guid>.Failure(SqlServerDataStoreResultCodes.ByName("ConnectionIdEmpty"));
        }

        var dataStore = new DataStoreImplementationConfiguration
        {
            Name = discovered.Name,
            Domain = "DataStore",
            Implementation = "MsSql",
            ConnectionId = connectionId,
            Paths = discovered.Paths.Select(p => CopyPath(p, new StringBuilder())).ToList(),
        };

        var saved = await _dataStoreProvider.Save(
            dataStore, dataStore.Domain, dataStore.Implementation, dataStore.Name, cancellationToken).ConfigureAwait(false);
        if (!saved.IsSuccess)
        {
            return saved.ToNewResult<Guid>();
        }

        // Save stamps the domain row's id onto the configuration it wrote.
        await UpdateConnectionAssociation(connectionId, dataStore.Id, cancellationToken).ConfigureAwait(false);

        SchemaImportPersisterLog.DataStorePersisted(_logger, discovered.Name, dataStore.Id);

        return GenericResult<Guid>.Success(dataStore.Id);
    }

    /// <inheritdoc />
    [ConventionOverride(MaxMethodLines = 75)]
    public async Task<IGenericResult<SchemaImportSyncResult>> Sync(
        Guid existingDataStoreId,
        DataStoreImplementationConfiguration discovered,
        CancellationToken cancellationToken = default)
    {
        if (existingDataStoreId == Guid.Empty)
        {
            return GenericResult<SchemaImportSyncResult>.Failure(SqlServerDataStoreResultCodes.ByName("ExistingDataStoreIdEmpty"));
        }

        if (discovered is null)
        {
            return GenericResult<SchemaImportSyncResult>.Failure(SqlServerDataStoreResultCodes.ByName("DataStoreNull"));
        }

        var loaded = await _dataStoreProvider.Get(existingDataStoreId, cancellationToken).ConfigureAwait(false);
        if (!loaded.IsSuccess || loaded.Value is null)
        {
            return loaded.ToNewResult<SchemaImportSyncResult>();
        }

        var dataStore = loaded.Value;
        var stats = new SyncStats();
        var schemaBuilder = new StringBuilder();

        var discoveredPathValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in discovered.Paths)
        {
            discoveredPathValues.Add(path.PathValue);

            var existingPath = dataStore.Paths.FirstOrDefault(
                p => string.Equals(p.PathValue, path.PathValue, StringComparison.OrdinalIgnoreCase));
            if (existingPath is not null)
            {
                SyncPath(existingPath, path, stats, schemaBuilder);
                continue;
            }

            dataStore.Paths.Add(CopyPath(path, schemaBuilder));
            stats.PathsAdded++;
        }

        stats.PathsRemoved += dataStore.Paths.RemoveAll(p => !discoveredPathValues.Contains(p.PathValue));

        var saved = await _dataStoreProvider.Save(
            dataStore, dataStore.Domain, dataStore.Implementation, dataStore.Name, cancellationToken).ConfigureAwait(false);
        if (!saved.IsSuccess)
        {
            return saved.ToNewResult<SchemaImportSyncResult>();
        }

        // TODO(FDW-235): the hash is computed and returned, but not yet stored on the MsSql store.
        var syncResult = new SchemaImportSyncResult
        {
            DataStoreId = existingDataStoreId,
            PathsAdded = stats.PathsAdded,
            PathsModified = stats.PathsModified,
            PathsRemoved = stats.PathsRemoved,
            ContainersAdded = stats.ContainersAdded,
            ContainersModified = stats.ContainersModified,
            ContainersRemoved = stats.ContainersRemoved,
            FieldsAdded = stats.FieldsAdded,
            FieldsModified = stats.FieldsModified,
            FieldsRemoved = stats.FieldsRemoved,
            NewSchemaHash = ComputeSchemaHash(schemaBuilder.ToString())
        };

        SchemaImportPersisterLog.DataStoreSynced(_logger, existingDataStoreId, syncResult.TotalChanges);

        return GenericResult<SchemaImportSyncResult>.Success(syncResult);
    }

    private static void SyncPath(
        DataPathConfiguration existingPath,
        DataPathConfiguration discoveredPath,
        SyncStats stats,
        StringBuilder schemaBuilder)
    {
        // Source description always follows discovery; it is not a structural modification.
        existingPath.SourceDescription = discoveredPath.SourceDescription;

        if (!string.Equals(existingPath.PathType, discoveredPath.PathType, StringComparison.Ordinal))
        {
            existingPath.PathType = discoveredPath.PathType;
            stats.PathsModified++;
        }

        schemaBuilder.AppendLine(CultureInfo.InvariantCulture, $"PATH:{discoveredPath.PathValue}");

        var discoveredContainerNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var container in discoveredPath.Containers)
        {
            discoveredContainerNames.Add(container.Name);

            var existingContainer = existingPath.Containers.FirstOrDefault(
                c => string.Equals(c.Name, container.Name, StringComparison.OrdinalIgnoreCase));
            if (existingContainer is not null)
            {
                SyncContainer(existingContainer, container, stats, schemaBuilder);
                continue;
            }

            existingPath.Containers.Add(CopyContainer(container, schemaBuilder));
            stats.ContainersAdded++;
        }

        stats.ContainersRemoved += existingPath.Containers.RemoveAll(c => !discoveredContainerNames.Contains(c.Name));
    }

    private static void SyncContainer(
        DataContainerConfiguration existingContainer,
        DataContainerConfiguration discoveredContainer,
        SyncStats stats,
        StringBuilder schemaBuilder)
    {
        // Only TypeId is structural for a container; descriptions are not.
        if (!string.Equals(existingContainer.TypeId, discoveredContainer.TypeId, StringComparison.Ordinal))
        {
            existingContainer.TypeId = discoveredContainer.TypeId;
            stats.ContainersModified++;
        }

        schemaBuilder.AppendLine(CultureInfo.InvariantCulture, $"CONTAINER:{discoveredContainer.Name}:{discoveredContainer.TypeId}");

        var discoveredFieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in discoveredContainer.Fields)
        {
            discoveredFieldNames.Add(field.Name);

            var existingField = existingContainer.Fields.FirstOrDefault(
                f => string.Equals(f.Name, field.Name, StringComparison.OrdinalIgnoreCase));
            if (existingField is null)
            {
                existingContainer.Fields.Add(CopyField(field));
                stats.FieldsAdded++;
            }
            else if (!string.Equals(existingField.DataType, field.DataType, StringComparison.Ordinal))
            {
                existingField.DataType = field.DataType;
                stats.FieldsModified++;
            }

            schemaBuilder.AppendLine(CultureInfo.InvariantCulture, $"FIELD:{field.Name}:{field.DataType}:{field.IsNullable}");
        }

        stats.FieldsRemoved += existingContainer.Fields.RemoveAll(f => !discoveredFieldNames.Contains(f.Name));
    }

    private static DataPathConfiguration CopyPath(DataPathConfiguration discoveredPath, StringBuilder schemaBuilder)
    {
        schemaBuilder.AppendLine(CultureInfo.InvariantCulture, $"PATH:{discoveredPath.PathValue}");

        return new DataPathConfiguration
        {
            Name = discoveredPath.Name,
            PathValue = discoveredPath.PathValue,
            PathType = discoveredPath.PathType,
            SourceDescription = discoveredPath.SourceDescription,
            Containers = discoveredPath.Containers.Select(c => CopyContainer(c, schemaBuilder)).ToList(),
        };
    }

    private static DataContainerConfiguration CopyContainer(DataContainerConfiguration discoveredContainer, StringBuilder schemaBuilder)
    {
        schemaBuilder.AppendLine(CultureInfo.InvariantCulture, $"CONTAINER:{discoveredContainer.Name}:{discoveredContainer.TypeId}");

        return new DataContainerConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = discoveredContainer.Name,
            TypeId = discoveredContainer.TypeId,
            Fields = discoveredContainer.Fields.Select(f =>
            {
                schemaBuilder.AppendLine(CultureInfo.InvariantCulture, $"FIELD:{f.Name}:{f.DataType}:{f.IsNullable}");
                return CopyField(f);
            }).ToList(),
        };
    }

    private static DataContainerFieldConfiguration CopyField(DataContainerFieldConfiguration field) => new()
    {
        Id = Guid.CreateVersion7(),
        Name = field.Name,
        DataType = field.DataType,
        IsNullable = field.IsNullable,
        Ordinal = field.Ordinal,
        IsSystemProvided = field.IsSystemProvided,
        MaxLength = field.MaxLength,
        Precision = field.Precision,
        Scale = field.Scale,
        DefaultValue = field.DefaultValue
    };

    private static string ComputeSchemaHash(string schemaContent)
    {
        var bytes = Encoding.UTF8.GetBytes(schemaContent);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToBase64String(hashBytes);
    }

    private async Task UpdateConnectionAssociation(Guid connectionId, Guid dataStoreId, CancellationToken cancellationToken)
    {
        var connectionResult = await _connectionProvider.Get(connectionId, cancellationToken).ConfigureAwait(false);
        if (!connectionResult.IsSuccess || connectionResult.Value is null)
        {
            SchemaImportPersisterLog.ConnectionUpdateFailed(_logger, connectionId, connectionResult.CurrentMessage);
            return;
        }

        if (connectionResult.Value is not MsSqlConnectionConfiguration msSql)
        {
            SchemaImportPersisterLog.ConnectionUpdateFailed(_logger, connectionId, "Connection is not an MsSql connection");
            return;
        }

        msSql.AssociatedDataStoreId = dataStoreId;
        msSql.LastSchemaImportDate = DateTimeOffset.UtcNow;

        var saveResult = await _connectionProvider.Save(
            msSql, msSql.Domain, msSql.Implementation, msSql.Name, cancellationToken).ConfigureAwait(false);
        if (!saveResult.IsSuccess)
        {
            SchemaImportPersisterLog.ConnectionUpdateFailed(_logger, connectionId, saveResult.CurrentMessage);
        }
    }

    private sealed class SyncStats
    {
        public int PathsAdded { get; set; }
        public int PathsModified { get; set; }
        public int PathsRemoved { get; set; }
        public int ContainersAdded { get; set; }
        public int ContainersModified { get; set; }
        public int ContainersRemoved { get; set; }
        public int FieldsAdded { get; set; }
        public int FieldsModified { get; set; }
        public int FieldsRemoved { get; set; }
    }
}
