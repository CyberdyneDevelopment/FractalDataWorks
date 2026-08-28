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
using Fdw.Services.Configuration;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Commands;
using Fdw.Services.Connections.MsSql;
using Fdw.Services.Data;
using Fdw.Services.Data.Commands;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.DataStores.SqlServer;

/// <summary>
/// Persists discovered SQL Server schema (a <see cref="DataStoreConfiguration"/>) to ManagedConfiguration tables.
/// </summary>
[ExcludeFromCodeCoverage] // Excluded: requires SQL Server connection
public sealed class MsSqlSchemaImportPersister : ISchemaImportPersister
{
    private readonly DataStoreConfigurationProvider _dataStoreProvider;
    private readonly ImplementationConfigurationProviderBase<DataPathConfiguration, DataPathConfigurationCommand> _dataPathProvider;
    private readonly ImplementationConfigurationProviderBase<DataContainerConfiguration, DataContainerConfigurationCommand> _containerProvider;
    private readonly ImplementationConfigurationProviderBase<DataContainerFieldConfiguration, DataContainerFieldConfigurationCommand> _fieldProvider;
    private readonly ConnectionConfigurationProvider _connectionProvider;
    private readonly ILogger<MsSqlSchemaImportPersister> _logger;

    /// <summary>Initializes a new instance of the <see cref="MsSqlSchemaImportPersister"/> class.</summary>
    public MsSqlSchemaImportPersister(
        DataStoreConfigurationProvider dataStoreProvider,
        ImplementationConfigurationProviderBase<DataPathConfiguration, DataPathConfigurationCommand> dataPathProvider,
        ImplementationConfigurationProviderBase<DataContainerConfiguration, DataContainerConfigurationCommand> containerProvider,
        ImplementationConfigurationProviderBase<DataContainerFieldConfiguration, DataContainerFieldConfigurationCommand> fieldProvider,
        ConnectionConfigurationProvider connectionProvider,
        ILogger<MsSqlSchemaImportPersister> logger)
    {
        _dataStoreProvider = dataStoreProvider ?? throw new ArgumentNullException(nameof(dataStoreProvider));
        _dataPathProvider = dataPathProvider ?? throw new ArgumentNullException(nameof(dataPathProvider));
        _containerProvider = containerProvider ?? throw new ArgumentNullException(nameof(containerProvider));
        _fieldProvider = fieldProvider ?? throw new ArgumentNullException(nameof(fieldProvider));
        _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
#pragma warning disable MA0051 // Method is too long - sequential persistence orchestration
    public async Task<IGenericResult<Guid>> Persist(
        DataStoreConfiguration discovered,
        Guid connectionId,
        CancellationToken cancellationToken = default)
#pragma warning restore MA0051
    {
        if (discovered is null)
        {
            return GenericResult<Guid>.Failure(SqlServerDataStoreResultCodes.ByName("DataStoreNull"));
        }

        if (connectionId == Guid.Empty)
        {
            return GenericResult<Guid>.Failure(SqlServerDataStoreResultCodes.ByName("ConnectionIdEmpty"));
        }

        var dataStoreConfig = new DataStoreConfiguration
        {
            Id = Guid.NewGuid(),
            Name = discovered.Name,
            ConnectionId = connectionId,
            ServiceType = "DataStore",
            ServiceOptionType = "MsSql",
            SectionName = "DataStores",
            Description = discovered.Description
        };

        var saveDataStoreResult = await _dataStoreProvider.Save(dataStoreConfig, cancellationToken).ConfigureAwait(false);
        if (!saveDataStoreResult.IsSuccess)
        {
            return GenericResult<Guid>.Failure(
                SqlServerDataStoreResultCodes.ByName("DataStoreSaveFailed"),
                ResultDetails.Create("error", saveDataStoreResult.CurrentMessage ?? "Unknown error"));
        }

        var dataStoreId = saveDataStoreResult.Value!.Id;

        // Persist paths, containers, and fields
        var schemaBuilder = new StringBuilder();
        foreach (var path in discovered.Paths)
        {
            var pathResult = await PersistPath(
                path,
                dataStoreId,
                schemaBuilder,
                cancellationToken).ConfigureAwait(false);

            if (!pathResult.IsSuccess)
            {
                SchemaImportPersisterLog.PathPersistFailed(_logger, path.Name, pathResult.CurrentMessage);
            }
        }

        // Update the parent Connection with the associated DataStore ID and timestamp
        await UpdateConnectionAssociation(connectionId, dataStoreId, cancellationToken).ConfigureAwait(false);

        SchemaImportPersisterLog.DataStorePersisted(_logger, discovered.Name, dataStoreId);

        return GenericResult<Guid>.Success(dataStoreId);
    }

    /// <inheritdoc />
    [ConventionOverride(MaxMethodLines = 75)]
    public async Task<IGenericResult<SchemaImportSyncResult>> Sync(
        Guid existingDataStoreId,
        DataStoreConfiguration discovered,
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

        // Load existing paths for comparison via DataStoreConfigurationProvider
        var existingPaths = await LoadExistingPaths(existingDataStoreId, cancellationToken).ConfigureAwait(false);

        var stats = new SyncStats();
        var schemaBuilder = new StringBuilder();

        // Process discovered paths
        var discoveredPathNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in discovered.Paths)
        {
            discoveredPathNames.Add(path.PathValue);

            if (existingPaths.TryGetValue(path.PathValue, out var existingPath))
            {
                // Update existing path
                var updateResult = await SyncPath(
                    existingPath,
                    path,
                    stats,
                    schemaBuilder,
                    cancellationToken).ConfigureAwait(false);

                if (!updateResult.IsSuccess)
                {
                    SchemaImportPersisterLog.PathSyncFailed(_logger, path.Name, updateResult.CurrentMessage);
                }
            }
            else
            {
                // Add new path
                var addResult = await PersistPath(
                    path,
                    existingDataStoreId,
                    schemaBuilder,
                    cancellationToken).ConfigureAwait(false);

                if (addResult.IsSuccess)
                {
                    stats.PathsAdded++;
                }
                else
                {
                    return addResult.ToNewResult<SchemaImportSyncResult>();
                }
            }
        }

        // Mark removed paths as deleted
        await DeleteRemovedPaths(
            existingPaths,
            discoveredPathNames,
            stats,
            cancellationToken).ConfigureAwait(false);

        var newSchemaHash = ComputeSchemaHash(schemaBuilder.ToString());

        // Update MsSqlDataStore hash and timestamp if schema changed
        await UpdateDataStoreSchemaHash(existingDataStoreId, newSchemaHash, cancellationToken).ConfigureAwait(false);

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
            NewSchemaHash = newSchemaHash
        };

        SchemaImportPersisterLog.DataStoreSynced(_logger, existingDataStoreId, syncResult.TotalChanges);

        return GenericResult<SchemaImportSyncResult>.Success(syncResult);
    }

    private async Task<IGenericResult> PersistPath(
        DataPathConfiguration discoveredPath,
        Guid dataStoreId,
        StringBuilder schemaBuilder,
        CancellationToken cancellationToken)
    {
        var pathConfig = new DataPathConfiguration
        {
            Id = Guid.NewGuid(),
            Name = discoveredPath.Name,
            DataStoreId = dataStoreId,
            PathValue = discoveredPath.PathValue,
            PathType = discoveredPath.PathType,
            SourceDescription = discoveredPath.SourceDescription
        };

        var savePathResult = await _dataPathProvider.Save(pathConfig, cancellationToken).ConfigureAwait(false);
        if (!savePathResult.IsSuccess)
        {
            return savePathResult;
        }

        var pathId = savePathResult.Value!.Id;
        schemaBuilder.AppendLine(CultureInfo.InvariantCulture, $"PATH:{discoveredPath.PathValue}");

        foreach (var container in discoveredPath.Containers)
        {
            var containerResult = await PersistContainer(
                container,
                pathId,
                schemaBuilder,
                cancellationToken).ConfigureAwait(false);

            if (!containerResult.IsSuccess)
            {
                SchemaImportPersisterLog.ContainerPersistFailed(
                    _logger,
                    container.Name,
                    containerResult.CurrentMessage);
            }
        }

        return GenericResult.Success();
    }

    private async Task<IGenericResult> PersistContainer(
        DataContainerConfiguration discoveredContainer,
        Guid dataPathId,
        StringBuilder schemaBuilder,
        CancellationToken cancellationToken)
    {
        var containerConfig = new DataContainerConfiguration
        {
            Id = Guid.NewGuid(),
            Name = discoveredContainer.Name,
            DataPathId = dataPathId,
            TypeId = discoveredContainer.TypeId
        };

        var saveContainerResult = await _containerProvider.Save(containerConfig, cancellationToken).ConfigureAwait(false);
        if (!saveContainerResult.IsSuccess)
        {
            return saveContainerResult;
        }

        var containerId = saveContainerResult.Value!.Id;
        schemaBuilder.AppendLine(CultureInfo.InvariantCulture, $"CONTAINER:{discoveredContainer.Name}:{discoveredContainer.TypeId}");

        // Persist fields
        foreach (var field in discoveredContainer.Fields)
        {
            var fieldConfig = new DataContainerFieldConfiguration
            {
                Id = Guid.NewGuid(),
                Name = field.Name,
                DataContainerId = containerId,
                DataType = field.DataType,
                IsNullable = field.IsNullable,
                Ordinal = field.Ordinal,
                IsSystemProvided = field.IsSystemProvided,
                MaxLength = field.MaxLength,
                Precision = field.Precision,
                Scale = field.Scale,
                DefaultValue = field.DefaultValue
            };

            var saveFieldResult = await _fieldProvider.Save(fieldConfig, cancellationToken).ConfigureAwait(false);
            if (!saveFieldResult.IsSuccess)
            {
                SchemaImportPersisterLog.FieldPersistFailed(
                    _logger,
                    field.Name,
                    saveFieldResult.CurrentMessage);
            }
            else
            {
                schemaBuilder.AppendLine(CultureInfo.InvariantCulture, $"FIELD:{field.Name}:{field.DataType}:{field.IsNullable}");
            }
        }

        return GenericResult.Success();
    }

#pragma warning disable MA0051 // Method is too long - sequential path sync with container iteration
    private async Task<IGenericResult> SyncPath(
        DataPathConfiguration existingPath,
        DataPathConfiguration discoveredPath,
        SyncStats stats,
        StringBuilder schemaBuilder,
        CancellationToken cancellationToken)
#pragma warning restore MA0051
    {
        var pathModified = false;

        // Always update SourceDescription from discovered metadata (does not count as structural modification)
        var sourceDescriptionChanged = !string.Equals(existingPath.SourceDescription, discoveredPath.SourceDescription, StringComparison.Ordinal);
        if (sourceDescriptionChanged)
        {
            existingPath.SourceDescription = discoveredPath.SourceDescription;
        }

        // Check if structural path properties changed (description changes are not structural)
        if (!string.Equals(existingPath.PathType, discoveredPath.PathType, StringComparison.Ordinal))
        {
            existingPath.PathType = discoveredPath.PathType;
            pathModified = true;
        }

        // Save if either structural or source description changed
        if (pathModified || sourceDescriptionChanged)
        {
            var updateResult = await _dataPathProvider.Save(existingPath, cancellationToken).ConfigureAwait(false);
            if (!updateResult.IsSuccess)
            {
                return updateResult;
            }
        }

        schemaBuilder.AppendLine(CultureInfo.InvariantCulture, $"PATH:{discoveredPath.PathValue}");

        // Sync containers from the cached DataPath hierarchy
        var existingContainers = LoadExistingContainers(existingPath);

        var discoveredContainerNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var container in discoveredPath.Containers)
        {
            discoveredContainerNames.Add(container.Name);

            if (existingContainers.TryGetValue(container.Name, out var existingContainer))
            {
                // Sync existing container
                var syncResult = await SyncContainer(
                    existingContainer,
                    container,
                    stats,
                    schemaBuilder,
                    cancellationToken).ConfigureAwait(false);

                if (!syncResult.IsSuccess)
                {
                    SchemaImportPersisterLog.ContainerSyncFailed(
                        _logger,
                        container.Name,
                        syncResult.CurrentMessage);
                }
            }
            else
            {
                // Add new container
                var addResult = await PersistContainer(
                    container,
                    existingPath.Id,
                    schemaBuilder,
                    cancellationToken).ConfigureAwait(false);

                if (addResult.IsSuccess)
                {
                    stats.ContainersAdded++;
                }
                else
                {
                    return addResult;
                }
            }
        }

        // Mark removed containers as deleted
        await DeleteRemovedContainers(
            existingContainers,
            discoveredContainerNames,
            stats,
            cancellationToken).ConfigureAwait(false);

        if (pathModified)
        {
            stats.PathsModified++;
        }

        return GenericResult.Success();
    }

    private async Task<IGenericResult> SyncContainer(
        DataContainerConfiguration existingContainer,
        DataContainerConfiguration discoveredContainer,
        SyncStats stats,
        StringBuilder schemaBuilder,
        CancellationToken cancellationToken)
    {
        var containerModified = false;

        // Check if structural container properties changed (only TypeId, not descriptions)
        if (!string.Equals(existingContainer.TypeId, discoveredContainer.TypeId, StringComparison.Ordinal))
        {
            existingContainer.TypeId = discoveredContainer.TypeId;
            containerModified = true;
        }

        // Save if structural properties changed
        if (containerModified)
        {
            var updateResult = await _containerProvider.Save(existingContainer, cancellationToken).ConfigureAwait(false);
            if (!updateResult.IsSuccess)
            {
                return updateResult;
            }
        }

        schemaBuilder.AppendLine(CultureInfo.InvariantCulture, $"CONTAINER:{discoveredContainer.Name}:{discoveredContainer.TypeId}");

        // Sync fields from the cached DataContainer hierarchy
        var existingFields = LoadExistingFields(existingContainer);

        var discoveredFieldNames = await SyncDiscoveredFields(
            discoveredContainer,
            existingContainer.Id,
            existingFields,
            stats,
            schemaBuilder,
            cancellationToken).ConfigureAwait(false);

        // Mark removed fields as deleted
        await DeleteRemovedFields(
            existingFields,
            discoveredFieldNames,
            stats,
            cancellationToken).ConfigureAwait(false);

        if (containerModified)
        {
            stats.ContainersModified++;
        }

        return GenericResult.Success();
    }

    private static string ComputeSchemaHash(string schemaContent)
    {
        var bytes = Encoding.UTF8.GetBytes(schemaContent);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToBase64String(hashBytes);
    }

    private async Task<Dictionary<string, DataPathConfiguration>> LoadExistingPaths(Guid dataStoreId, CancellationToken cancellationToken)
    {
        var existingPaths = new Dictionary<string, DataPathConfiguration>(StringComparer.OrdinalIgnoreCase);
        var dataStoreResult = await _dataStoreProvider.Get(dataStoreId, cancellationToken).ConfigureAwait(false);
        if (!dataStoreResult.IsSuccess || dataStoreResult.Value == null)
        {
            return existingPaths;
        }

        foreach (var path in dataStoreResult.Value.Paths)
        {
            if (!string.IsNullOrEmpty(path.PathValue))
            {
                existingPaths[path.PathValue] = path;
            }
        }
        return existingPaths;
    }

    private async Task DeleteRemovedPaths(
        Dictionary<string, DataPathConfiguration> existingPaths,
        HashSet<string> discoveredPathNames,
        SyncStats stats,
        CancellationToken cancellationToken)
    {
        foreach (var existingPath in existingPaths.Values)
        {
            if (!discoveredPathNames.Contains(existingPath.PathValue))
            {
                var deleteResult = await _dataPathProvider.Delete(existingPath.Id, cancellationToken).ConfigureAwait(false);
                if (deleteResult.IsSuccess)
                {
                    stats.PathsRemoved++;
                }
            }
        }
    }

    private static Dictionary<string, DataContainerConfiguration> LoadExistingContainers(DataPathConfiguration path)
    {
        var existingContainers = new Dictionary<string, DataContainerConfiguration>(StringComparer.OrdinalIgnoreCase);
        foreach (var container in path.Containers)
        {
            if (!string.IsNullOrEmpty(container.Name))
            {
                existingContainers[container.Name] = container;
            }
        }
        return existingContainers;
    }

    private async Task DeleteRemovedContainers(
        Dictionary<string, DataContainerConfiguration> existingContainers,
        HashSet<string> discoveredContainerNames,
        SyncStats stats,
        CancellationToken cancellationToken)
    {
        foreach (var existingContainer in existingContainers.Values)
        {
            if (!discoveredContainerNames.Contains(existingContainer.Name))
            {
                var deleteResult = await _containerProvider.Delete(existingContainer.Id, cancellationToken).ConfigureAwait(false);
                if (deleteResult.IsSuccess)
                {
                    stats.ContainersRemoved++;
                }
            }
        }
    }

    private static Dictionary<string, DataContainerFieldConfiguration> LoadExistingFields(DataContainerConfiguration container)
    {
        var existingFields = new Dictionary<string, DataContainerFieldConfiguration>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in container.Fields)
        {
            if (!string.IsNullOrEmpty(field.Name))
            {
                existingFields[field.Name] = field;
            }
        }
        return existingFields;
    }

    [ConventionOverride(MaxCyclomaticComplexity = 20)]
    private async Task<HashSet<string>> SyncDiscoveredFields(
        DataContainerConfiguration discoveredContainer,
        Guid containerId,
        Dictionary<string, DataContainerFieldConfiguration> existingFields,
        SyncStats stats,
        StringBuilder schemaBuilder,
        CancellationToken cancellationToken)
    {
        var discoveredFieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var field in discoveredContainer.Fields)
        {
            discoveredFieldNames.Add(field.Name);

            if (existingFields.TryGetValue(field.Name, out var existingField))
            {
                // Check if structural field properties changed
                var structuralChanged = !string.Equals(existingField.DataType, field.DataType, StringComparison.Ordinal);

                if (structuralChanged)
                {
                    existingField.DataType = field.DataType;

                    var updateResult = await _fieldProvider.Save(existingField, cancellationToken).ConfigureAwait(false);
                    if (updateResult.IsSuccess)
                    {
                        stats.FieldsModified++;
                    }
                }
            }
            else
            {
                // Add new field
                var fieldConfig = new DataContainerFieldConfiguration
                {
                    Id = Guid.NewGuid(),
                    Name = field.Name,
                    DataContainerId = containerId,
                    DataType = field.DataType,
                    IsNullable = field.IsNullable,
                    Ordinal = field.Ordinal,
                    IsSystemProvided = field.IsSystemProvided,
                    MaxLength = field.MaxLength,
                    Precision = field.Precision,
                    Scale = field.Scale,
                    DefaultValue = field.DefaultValue
                };

                var saveResult = await _fieldProvider.Save(fieldConfig, cancellationToken).ConfigureAwait(false);
                if (saveResult.IsSuccess)
                {
                    stats.FieldsAdded++;
                }
            }

            schemaBuilder.AppendLine(CultureInfo.InvariantCulture, $"FIELD:{field.Name}:{field.DataType}:{field.IsNullable}");
        }

        return discoveredFieldNames;
    }

    private async Task DeleteRemovedFields(
        Dictionary<string, DataContainerFieldConfiguration> existingFields,
        HashSet<string> discoveredFieldNames,
        SyncStats stats,
        CancellationToken cancellationToken)
    {
        foreach (var existingField in existingFields.Values)
        {
            if (!discoveredFieldNames.Contains(existingField.Name))
            {
                var deleteResult = await _fieldProvider.Delete(existingField.Id, cancellationToken).ConfigureAwait(false);
                if (deleteResult.IsSuccess)
                {
                    stats.FieldsRemoved++;
                }
            }
        }
    }

    private async Task UpdateDataStoreSchemaHash(Guid dataStoreId, string newSchemaHash, CancellationToken cancellationToken)
    {
        var dataStoreResult = await _dataStoreProvider.Get(dataStoreId, cancellationToken).ConfigureAwait(false);
        if (!dataStoreResult.IsSuccess || dataStoreResult.Value == null)
        {
            return;
        }

        // DataStoreConfigurationProvider returns DataStoreConfiguration (general model), not MsSqlDataStoreConfiguration.
        // Schema hash tracking requires IServiceConfigurationProvider<MsSqlDataStoreConfiguration>.
        // TODO(FDW-235): Restore schema hash tracking after the FDW-220/221 cache migration.
        _ = dataStoreResult.Value;
        _ = newSchemaHash;
    }

    private async Task UpdateConnectionAssociation(Guid connectionId, Guid dataStoreId, CancellationToken cancellationToken)
    {
        var connectionResult = await _connectionProvider.Get(connectionId, cancellationToken).ConfigureAwait(false);
        if (!connectionResult.IsSuccess || connectionResult.Value is null)
        {
            SchemaImportPersisterLog.ConnectionUpdateFailed(_logger, connectionId, connectionResult.CurrentMessage);
            return;
        }

        var connectionConfig = connectionResult.Value;
        if (connectionConfig.Configuration is not MsSqlConnectionConfiguration msSqlBody)
        {
            SchemaImportPersisterLog.ConnectionUpdateFailed(_logger, connectionId, "Connection has no MsSql typed body");
            return;
        }

        msSqlBody.AssociatedDataStoreId = dataStoreId;
        msSqlBody.LastSchemaImportDate = DateTimeOffset.UtcNow;

        var saveResult = await _connectionProvider.Save(connectionConfig, cancellationToken).ConfigureAwait(false);
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
