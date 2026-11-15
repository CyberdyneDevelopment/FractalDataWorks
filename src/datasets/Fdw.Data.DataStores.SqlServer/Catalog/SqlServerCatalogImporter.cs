using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataStores.SqlServer.Logging;
using Fdw.Data.MsSql;
using Fdw.Data.SchemaImporters.Abstractions.Configuration;
using Fdw.Results;
using Fdw.Services.Connections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.DataStores.SqlServer.Catalog;

/// <summary>
/// Builds a discovered <see cref="DataStoreImplementationConfiguration"/> (one path per schema, a container per table, view and procedure, a
/// field per column or parameter) from a <see cref="SqlServerCatalogReader"/> (FDW-5876). It is the importer's work with no SqlClient in it:
/// the reader reads the catalog through a connection and this builds the result. Where the SqlClient importer carried on after a failed read
/// (a view or procedure list that would not load became a warning and an empty list, a table whose columns would not load was skipped, a
/// procedure whose parameters would not load got no fields), a failed read here fails the import, so a smaller store is never mistaken for the
/// database's.
/// </summary>
public sealed class SqlServerCatalogImporter
{
    private readonly SqlServerCatalogReader _reader;
    private readonly ILogger<SqlServerCatalogImporter> _logger;

    /// <summary>Initializes a new instance of the <see cref="SqlServerCatalogImporter"/> class.</summary>
    /// <param name="reader">The reader the catalog is read with.</param>
    /// <param name="logger">The logger.</param>
    public SqlServerCatalogImporter(SqlServerCatalogReader reader, ILogger<SqlServerCatalogImporter>? logger)
    {
        _reader = reader;
        _logger = logger ?? NullLogger<SqlServerCatalogImporter>.Instance;
    }

    /// <summary>Imports the schema of the database the reader's connection is on.</summary>
    /// <param name="options">The import options; the schema filters and SkipStoredProcedures apply, as the SqlClient importer applied them.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The discovered data store, or the failure of the first read that failed.</returns>
    public async Task<IGenericResult<DataStoreImplementationConfiguration>> Import(SchemaImporterOptions options, CancellationToken cancellationToken = default)
    {
        var database = await _reader.ReadDatabaseName(cancellationToken).ConfigureAwait(false);
        if (database.IsFailure)
        {
            return database.ToNewResult<DataStoreImplementationConfiguration>();
        }

        SqlServerCatalogLog.ImportStarted(_logger, database.Value ?? string.Empty);
        var dataStore = new DataStoreImplementationConfiguration { Name = database.Value ?? string.Empty, Implementation = "MsSql" };
        var count = 0;

        var tables = await _reader.ReadTables(cancellationToken).ConfigureAwait(false);
        if (tables.IsFailure)
        {
            return tables.ToNewResult<DataStoreImplementationConfiguration>();
        }

        var added = await AddObjects(dataStore, Filter(tables.Value ?? [], options), "Table", cancellationToken).ConfigureAwait(false);
        if (added.IsFailure)
        {
            return added.ToNewResult<DataStoreImplementationConfiguration>();
        }

        count += added.Value;

        var views = await _reader.ReadViews(cancellationToken).ConfigureAwait(false);
        if (views.IsFailure)
        {
            return views.ToNewResult<DataStoreImplementationConfiguration>();
        }

        added = await AddObjects(dataStore, Filter(views.Value ?? [], options), "View", cancellationToken).ConfigureAwait(false);
        if (added.IsFailure)
        {
            return added.ToNewResult<DataStoreImplementationConfiguration>();
        }

        count += added.Value;

        // Why: the SqlClient importer made no container for a procedure when SkipStoredProcedures was set; reading the list to throw it away is not needed.
        if (!options.SkipStoredProcedures)
        {
            var procedures = await _reader.ReadProcedures(cancellationToken).ConfigureAwait(false);
            if (procedures.IsFailure)
            {
                return procedures.ToNewResult<DataStoreImplementationConfiguration>();
            }

            added = await AddObjects(dataStore, Filter(procedures.Value ?? [], options), "StoredProcedure", cancellationToken).ConfigureAwait(false);
            if (added.IsFailure)
            {
                return added.ToNewResult<DataStoreImplementationConfiguration>();
            }

            count += added.Value;
        }

        SqlServerCatalogLog.ImportCompleted(_logger, dataStore.Name, count);
        return GenericResult<DataStoreImplementationConfiguration>.Success(dataStore);
    }

    /// <summary>Checks that the catalog of the reader's connection can be read, by reading the database name.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True when it can, or the failure of the read.</returns>
    public async Task<IGenericResult<bool>> Validate(CancellationToken cancellationToken = default)
    {
        var database = await _reader.ReadDatabaseName(cancellationToken).ConfigureAwait(false);
        return database.IsFailure
            ? database.ToNewResult<bool>()
            : GenericResult<bool>.Success(true);
    }

    // The schema filters of the options, as the SqlClient importer applied them to each list: an include list keeps only the schemas in it,
    // an exclude list drops the schemas in it, both compared without regard to case.
    private static List<CatalogObject> Filter(IReadOnlyList<CatalogObject> objects, SchemaImporterOptions options)
        =>
        [
            .. objects.Where(o =>
                (options.IncludeSchemas is not { Count: > 0 } include || include.Contains(o.SchemaName, StringComparer.OrdinalIgnoreCase))
                && (options.ExcludeSchemas is not { Count: > 0 } exclude || !exclude.Contains(o.SchemaName, StringComparer.OrdinalIgnoreCase))),
        ];

    // One container per object, in the path named for its schema (made when the first object of that schema comes), its fields the object's
    // columns, or for a procedure its parameters.
    private async Task<IGenericResult<int>> AddObjects(
        DataStoreImplementationConfiguration dataStore,
        List<CatalogObject> objects,
        string containerTypeId,
        CancellationToken cancellationToken)
    {
        foreach (var item in objects)
        {
            var columns = await (string.Equals(containerTypeId, "StoredProcedure", StringComparison.Ordinal)
                ? _reader.ReadParameters(item.SchemaName, item.ObjectName, cancellationToken)
                : _reader.ReadColumns(item.SchemaName, item.ObjectName, cancellationToken)).ConfigureAwait(false);
            if (columns.IsFailure)
            {
                return columns.ToNewResult<int>();
            }

            var path = dataStore.Paths.FirstOrDefault(p => string.Equals(p.Name, item.SchemaName, StringComparison.Ordinal));
            if (path is null)
            {
                path = new DataPathConfiguration
                {
                    Id = Guid.NewGuid(),
                    Name = item.SchemaName,
                    PathValue = string.Empty,
                    PathType = "DatabasePath",
                    SourceDescription = null,
                };
                dataStore.Paths.Add(path);
            }

            path.Containers.Add(BuildContainer(item.ObjectName, containerTypeId, columns.Value ?? []));
        }

        return GenericResult<int>.Success(objects.Count);
    }

    private static DataContainerConfiguration BuildContainer(string objectName, string containerTypeId, IReadOnlyList<CatalogColumn> columns)
    {
        var container = new DataContainerConfiguration { Id = Guid.NewGuid(), Name = objectName, TypeId = containerTypeId };
        for (var ordinal = 0; ordinal < columns.Count; ordinal++)
        {
            container.Fields.Add(MapColumnToField(columns[ordinal], ordinal));
        }

        return container;
    }

    private static DataContainerFieldConfiguration MapColumnToField(CatalogColumn column, int ordinal)
    {
        var clrType = MsSqlConverters.BySourceType(column.DataType.ToLowerInvariant()).TargetClrType;
        if (column.IsNullable && clrType.IsValueType)
        {
            clrType = typeof(Nullable<>).MakeGenericType(clrType);
        }

        return new DataContainerFieldConfiguration
        {
            Id = Guid.NewGuid(),
            Name = column.Name,
            DataType = clrType.Name,
            IsNullable = column.IsNullable,
            Ordinal = ordinal,
            IsSystemProvided = column.IsIdentity || column.IsComputed,
        };
    }
}
