using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Conventions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Data.DataStores.SqlServer.Logging;
using Fdw.Data.DataStores.SqlServer.Results;
using Fdw.Data.MsSql;
using Fdw.Data.SchemaImporters.Abstractions;
using Fdw.Data.SchemaImporters.Abstractions.Configuration;
using Fdw.Results;
using Fdw.Results.Abstractions;
using Fdw.Services.Connections;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.DataStores.SqlServer;

/// <summary>
/// Imports schema from SQL Server databases by querying INFORMATION_SCHEMA views.
/// Returns a discovered <see cref="DataStoreConfiguration"/> with DatabasePath rows containing
/// Table/View/StoredProcedure container configs and their field configs.
/// </summary>
[TypeOption(typeof(SchemaImporters.Abstractions.SchemaImporters), "SqlServer", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage] // Excluded: requires SQL Server connection
public sealed partial class SqlServerSchemaImporter : SchemaImporterBase<SqlServerConfiguration>, ISchemaImporter<SqlServerConfiguration>
{
    private readonly ILogger<SqlServerSchemaImporter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerSchemaImporter"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for diagnostic output.</param>
    public SqlServerSchemaImporter(ILogger<SqlServerSchemaImporter> logger)
        : base(
            id: 1,
            name: "SqlServer",
            description: "Imports schema from SQL Server databases via INFORMATION_SCHEMA",
            dataStoreType: "SqlServer")
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region ISchemaImporter Implementation

    /// <inheritdoc/>
    [ConventionOverride(MaxMethodLines = 70)]
    public override async Task<IGenericResult<DataStoreConfiguration>> Import(
        string source,
        SchemaImporterOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(source))
                return GenericResult<DataStoreConfiguration>.Failure(
                    SqlServerDataStoreResultCodes.ByName("ConnectionStringEmpty"));

            var connectionBuilder = new SqlConnectionStringBuilder(source);
            SqlServerSchemaImporterLogger.ImportStarted(_logger, connectionBuilder.DataSource, connectionBuilder.InitialCatalog);

            // 1. Open connection
            var connection = new SqlConnection(source);
            await using (connection.ConfigureAwait(false))
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                var databaseName = connection.Database;

                // 2. Discover tables, views, and stored procedures
                var tablesResult = await DiscoverTables(connection, options, cancellationToken).ConfigureAwait(false);
                if (!tablesResult.IsSuccess)
                    return GenericResult<DataStoreConfiguration>.Failure(
                        SqlServerDataStoreResultCodes.ByName("DiscoverTablesFailed"),
                        ResultDetails.Create("error", tablesResult.CurrentMessage ?? "Unknown error"));

                var viewsResult = await DiscoverViews(connection, options, cancellationToken).ConfigureAwait(false);
                if (!viewsResult.IsSuccess)
                    SqlServerSchemaImporterLogger.Warning(_logger, "Failed to discover views", viewsResult.CurrentMessage);

                var sprocsResult = await DiscoverStoredProcedures(connection, options, cancellationToken).ConfigureAwait(false);
                if (!sprocsResult.IsSuccess)
                    SqlServerSchemaImporterLogger.Warning(_logger, "Failed to discover stored procedures", sprocsResult.CurrentMessage);

                // 3. Build the discovered DataStore configuration directly (no legacy IDataStore tree)
                var dataStore = new DataStoreConfiguration
                {
                    Name = databaseName,
                    ServiceType = "DataStore",
                    ServiceOptionType = "MsSql",
                    SectionName = "DataStores"
                };

                var totalObjects = 0;

                // Process tables (all CRUD operations)
                totalObjects += await AddDiscoveredPaths(
                    connection, dataStore, tablesResult, "Table", CreateTableContainer, options, cancellationToken).ConfigureAwait(false);

                // Process views (read-only)
                totalObjects += await AddDiscoveredPaths(
                    connection, dataStore, viewsResult, "View", CreateViewContainer, options, cancellationToken).ConfigureAwait(false);

                // Process stored procedures (read-only)
                totalObjects += await AddDiscoveredPaths(
                    connection, dataStore, sprocsResult, "StoredProcedure", CreateStoredProcedureContainer, options, cancellationToken).ConfigureAwait(false);

                SqlServerSchemaImporterLogger.ImportCompleted(_logger, databaseName, totalObjects);

                return GenericResult<DataStoreConfiguration>.Success(dataStore);
            }
        }
        catch (Exception ex)
        {
            return GenericResult<DataStoreConfiguration>.Failure(
                SqlServerSchemaImporterLogger.ImportFailed(_logger, ex));
        }
    }

    /// <summary>
    /// Validates the SQL Server connection string by attempting to open a connection.
    /// </summary>
    /// <param name="source">The SQL Server connection string.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    public override async Task<IGenericResult<bool>> Validate(
        string source,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connection = new SqlConnection(source);
            await using (connection.ConfigureAwait(false))
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                return GenericResult<bool>.Success(true);
            }
        }
        catch (Exception ex)
        {
            return GenericResult<bool>.Failure(
                SqlServerDataStoreResultCodes.ByName("InvalidConnectionString"),
                ResultDetails.Create("error", ex.Message));
        }
    }

    #endregion

    #region Table Discovery

    private static Task<IGenericResult<List<DatabaseObjectInfo>>> DiscoverTables(
        SqlConnection connection,
        SchemaImporterOptions? options,
        CancellationToken cancellationToken)
    {
        const string query = @"
            SELECT
                TABLE_SCHEMA,
                TABLE_NAME,
                'BASE TABLE' AS TABLE_TYPE
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_TYPE = 'BASE TABLE'
                AND TABLE_SCHEMA NOT IN ('sys', 'INFORMATION_SCHEMA')
            ORDER BY TABLE_SCHEMA, TABLE_NAME";

        return ExecuteDiscoveryQuery(connection, query, options, cancellationToken);
    }

    private async Task<IGenericResult<DataContainerConfiguration>> CreateTableContainer(
        SqlConnection connection,
        DatabaseObjectInfo table,
        SchemaImporterOptions? options,
        CancellationToken cancellationToken)
    {
        try
        {
            // 1. Get columns for this table
            var columnsResult = await GetColumns(
                connection,
                table.SchemaName,
                table.ObjectName,
                cancellationToken).ConfigureAwait(false);

            if (!columnsResult.IsSuccess || columnsResult.Value == null)
            {
                return GenericResult<DataContainerConfiguration>.Failure(
                    SqlServerSchemaImporterLogger.TableSkipped(_logger, table.SchemaName, table.ObjectName, columnsResult.CurrentMessage));
            }

            // 2. Build the container config (Table: all CRUD operations) with its field configs
            return GenericResult<DataContainerConfiguration>.Success(
                BuildContainer(table.ObjectName, "Table", columnsResult.Value));
        }
        catch (Exception ex)
        {
            return GenericResult<DataContainerConfiguration>.Failure(
                SqlServerSchemaImporterLogger.TableError(_logger, table.SchemaName, table.ObjectName, ex));
        }
    }

    #endregion

    #region View Discovery

    private static Task<IGenericResult<List<DatabaseObjectInfo>>> DiscoverViews(
        SqlConnection connection,
        SchemaImporterOptions? options,
        CancellationToken cancellationToken)
    {
        const string query = @"
            SELECT
                TABLE_SCHEMA,
                TABLE_NAME,
                'VIEW' AS TABLE_TYPE
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_TYPE = 'VIEW'
                AND TABLE_SCHEMA NOT IN ('sys', 'INFORMATION_SCHEMA')
            ORDER BY TABLE_SCHEMA, TABLE_NAME";

        return ExecuteDiscoveryQuery(connection, query, options, cancellationToken);
    }

    private async Task<IGenericResult<DataContainerConfiguration>> CreateViewContainer(
        SqlConnection connection,
        DatabaseObjectInfo view,
        SchemaImporterOptions? options,
        CancellationToken cancellationToken)
    {
        try
        {
            var columnsResult = await GetColumns(
                connection,
                view.SchemaName,
                view.ObjectName,
                cancellationToken).ConfigureAwait(false);

            if (!columnsResult.IsSuccess || columnsResult.Value == null)
            {
                return GenericResult<DataContainerConfiguration>.Failure(
                    SqlServerSchemaImporterLogger.TableSkipped(_logger, view.SchemaName, view.ObjectName, columnsResult.CurrentMessage));
            }

            // View - read-only
            return GenericResult<DataContainerConfiguration>.Success(
                BuildContainer(view.ObjectName, "View", columnsResult.Value));
        }
        catch (Exception ex)
        {
            return GenericResult<DataContainerConfiguration>.Failure(
                SqlServerSchemaImporterLogger.TableError(_logger, view.SchemaName, view.ObjectName, ex));
        }
    }

    #endregion

    #region Stored Procedure Discovery

    private static Task<IGenericResult<List<DatabaseObjectInfo>>> DiscoverStoredProcedures(
        SqlConnection connection,
        SchemaImporterOptions? options,
        CancellationToken cancellationToken)
    {
        const string query = @"
            SELECT
                ROUTINE_SCHEMA AS TABLE_SCHEMA,
                ROUTINE_NAME AS TABLE_NAME,
                'PROCEDURE' AS TABLE_TYPE
            FROM INFORMATION_SCHEMA.ROUTINES
            WHERE ROUTINE_TYPE = 'PROCEDURE'
                AND ROUTINE_SCHEMA NOT IN ('sys')
            ORDER BY ROUTINE_SCHEMA, ROUTINE_NAME";

        return ExecuteDiscoveryQuery(connection, query, options, cancellationToken);
    }

    private async Task<IGenericResult<DataContainerConfiguration>> CreateStoredProcedureContainer(
        SqlConnection connection,
        DatabaseObjectInfo sproc,
        SchemaImporterOptions? options,
        CancellationToken cancellationToken)
    {
        try
        {
            // Note: Getting result set schema from stored procedures is complex
            // Would require executing with FMTONLY or parsing definition
            // For now, surface the discovered parameters as the container's fields.
            if (options?.SkipStoredProcedures == true)
            {
                return GenericResult<DataContainerConfiguration>.Failure(SqlServerDataStoreResultCodes.ByName("StoredProceduresSkipped"));
            }

            // Get parameters
            var parametersResult = await GetProcedureParameters(
                connection,
                sproc.SchemaName,
                sproc.ObjectName,
                cancellationToken).ConfigureAwait(false);

            var parameters = parametersResult.IsSuccess && parametersResult.Value != null
                ? parametersResult.Value
                : new List<ColumnInfo>();

            // Why: the result-set schema is unknowable without execution; surface the discovered
            // parameters as the container's fields so the discovered metadata is not dropped.
            return GenericResult<DataContainerConfiguration>.Success(
                BuildContainer(sproc.ObjectName, "StoredProcedure", parameters));
        }
        catch (Exception ex)
        {
            return GenericResult<DataContainerConfiguration>.Failure(
                SqlServerSchemaImporterLogger.TableError(_logger, sproc.SchemaName, sproc.ObjectName, ex));
        }
    }

    #endregion

    #region Column Discovery

#pragma warning disable MA0051 // Method is too long - SQL query string literal, not complex
    private static async Task<IGenericResult<List<ColumnInfo>>> GetColumns(
        SqlConnection connection,
        string schemaName,
        string tableName,
        CancellationToken cancellationToken)
#pragma warning restore MA0051
    {
        const string query = @"
            SELECT
                c.COLUMN_NAME AS ColumnName,
                c.DATA_TYPE AS DataType,
                c.IS_NULLABLE AS IsNullable,
                c.CHARACTER_MAXIMUM_LENGTH AS MaxLength,
                c.NUMERIC_PRECISION AS Precision,
                c.NUMERIC_SCALE AS Scale,
                c.COLUMN_DEFAULT AS DefaultValue,
                CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END AS IsPrimaryKey,
                COLUMNPROPERTY(OBJECT_ID(c.TABLE_SCHEMA + '.' + c.TABLE_NAME), c.COLUMN_NAME, 'IsIdentity') AS IsIdentity,
                COLUMNPROPERTY(OBJECT_ID(c.TABLE_SCHEMA + '.' + c.TABLE_NAME), c.COLUMN_NAME, 'IsComputed') AS IsComputed
            FROM INFORMATION_SCHEMA.COLUMNS c
            LEFT JOIN (
                SELECT ku.TABLE_SCHEMA, ku.TABLE_NAME, ku.COLUMN_NAME
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
                    ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
                WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
            ) pk ON c.TABLE_SCHEMA = pk.TABLE_SCHEMA
                AND c.TABLE_NAME = pk.TABLE_NAME
                AND c.COLUMN_NAME = pk.COLUMN_NAME
            WHERE c.TABLE_SCHEMA = @SchemaName
                AND c.TABLE_NAME = @TableName
            ORDER BY c.ORDINAL_POSITION";

        try
        {
            var columns = new List<ColumnInfo>();

            var command = new SqlCommand(query, connection);
            await using (command.ConfigureAwait(false))
            {
                command.Parameters.AddWithValue("@SchemaName", schemaName);
                command.Parameters.AddWithValue("@TableName", tableName);

                var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                await using (reader.ConfigureAwait(false))
                {

                    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        columns.Add(new ColumnInfo
                        {
                            Name = reader.GetString(0),
                            DataType = reader.GetString(1),
                            IsNullable = string.Equals(reader.GetString(2), "YES", StringComparison.Ordinal),
                            MaxLength = await reader.IsDBNullAsync(3, cancellationToken).ConfigureAwait(false) ? (int?)null : reader.GetInt32(3),
                            Precision = await reader.IsDBNullAsync(4, cancellationToken).ConfigureAwait(false) ? (byte?)null : reader.GetByte(4),
                            Scale = await reader.IsDBNullAsync(5, cancellationToken).ConfigureAwait(false) ? (int?)null : reader.GetInt32(5),
                            DefaultValue = await reader.IsDBNullAsync(6, cancellationToken).ConfigureAwait(false) ? null : reader.GetString(6),
                            IsPrimaryKey = reader.GetInt32(7) == 1,
                            IsIdentity = reader.GetInt32(8) == 1,
                            IsComputed = reader.GetInt32(9) == 1
                        });
                    }

                    return GenericResult<List<ColumnInfo>>.Success(columns);
                }
            }
        }
        catch (Exception ex)
        {
            return GenericResult<List<ColumnInfo>>.Failure(
                SqlServerDataStoreResultCodes.ByName("GetColumnsFailed"),
                ResultDetails.Create("error", ex.Message));
        }
    }

    private static async Task<IGenericResult<List<ColumnInfo>>> GetProcedureParameters(
        SqlConnection connection,
        string schemaName,
        string procedureName,
        CancellationToken cancellationToken)
    {
        const string query = @"
            SELECT
                PARAMETER_NAME AS ColumnName,
                DATA_TYPE AS DataType,
                CASE WHEN PARAMETER_MODE = 'IN' OR PARAMETER_MODE IS NULL THEN 'YES' ELSE 'NO' END AS IsNullable,
                CHARACTER_MAXIMUM_LENGTH AS MaxLength,
                NUMERIC_PRECISION AS Precision,
                NUMERIC_SCALE AS Scale
            FROM INFORMATION_SCHEMA.PARAMETERS
            WHERE SPECIFIC_SCHEMA = @SchemaName
                AND SPECIFIC_NAME = @ProcedureName
            ORDER BY ORDINAL_POSITION";

        try
        {
            var parameters = new List<ColumnInfo>();

            var command = new SqlCommand(query, connection);
            await using (command.ConfigureAwait(false))
            {
                command.Parameters.AddWithValue("@SchemaName", schemaName);
                command.Parameters.AddWithValue("@ProcedureName", procedureName);

                var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                await using (reader.ConfigureAwait(false))
                {

                    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        parameters.Add(new ColumnInfo
                        {
                            Name = reader.GetString(0),
                            DataType = reader.GetString(1),
                            IsNullable = string.Equals(reader.GetString(2), "YES", StringComparison.Ordinal),
                            MaxLength = await reader.IsDBNullAsync(3, cancellationToken).ConfigureAwait(false) ? (int?)null : reader.GetInt32(3),
                            Precision = await reader.IsDBNullAsync(4, cancellationToken).ConfigureAwait(false) ? (byte?)null : reader.GetByte(4),
                            Scale = await reader.IsDBNullAsync(5, cancellationToken).ConfigureAwait(false) ? (int?)null : reader.GetInt32(5)
                        });
                    }

                    return GenericResult<List<ColumnInfo>>.Success(parameters);
                }
            }
        }
        catch (Exception ex)
        {
            return GenericResult<List<ColumnInfo>>.Failure(
                SqlServerDataStoreResultCodes.ByName("GetParametersFailed"),
                ResultDetails.Create("error", ex.Message));
        }
    }

    #endregion

    #region Container / Field Building

    // Why: The persister minted a Guid Id per discovered DataStore/Path/Container/Field row. Preserve
    // that by minting Ids on the config emitted here so the discovered side carries durable identity.
    private static DataContainerConfiguration BuildContainer(string objectName, string containerTypeId, List<ColumnInfo> columns)
    {
        var container = new DataContainerConfiguration
        {
            Id = Guid.NewGuid(),
            Name = objectName,
            // Why: TypeId is the container-type discriminator (Table/View/StoredProcedure) — the
            // table/view/sproc → supported-ops intent is carried by this discriminator.
            TypeId = containerTypeId
        };

        var ordinal = 0;
        foreach (var column in columns)
        {
            container.Fields.Add(MapColumnToFieldConfig(column, ordinal));
            ordinal++;
        }

        return container;
    }

    private static DataContainerFieldConfiguration MapColumnToFieldConfig(ColumnInfo column, int ordinal)
    {
        // Why: SINGLE lookup using MsSql converters (fixes the historical double-lookup issue).
        var converter = MsSqlConverters.BySourceType(column.DataType.ToLowerInvariant());

        var clrType = converter.TargetClrType;

        // Make nullable if needed
        if (column.IsNullable && clrType.IsValueType)
        {
            clrType = typeof(Nullable<>).MakeGenericType(clrType);
        }

        return new DataContainerFieldConfiguration
        {
            Id = Guid.NewGuid(),
            Name = column.Name,
            // Why: DataType carries the resolved CLR type name — identical to what the legacy
            // persister wrote (field.FieldType.TypeName), preserving persisted output.
            DataType = clrType.Name,
            IsNullable = column.IsNullable,
            Ordinal = ordinal,
            // Why: IDENTITY / COMPUTED columns are system-provided and excluded from INSERTs.
            IsSystemProvided = column.IsIdentity || column.IsComputed
        };
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Adds containers from discovered database objects to the DataStore configuration.
    /// One DataPath per object (schema.object), each holding a single container.
    /// </summary>
    /// <param name="connection">The SQL connection.</param>
    /// <param name="dataStore">The DataStore configuration being assembled.</param>
    /// <param name="discoveryResult">The result containing discovered objects.</param>
    /// <param name="containerTypeId">The container-type discriminator (Table/View/StoredProcedure).</param>
    /// <param name="containerCreator">The delegate to create a container config for each object.</param>
    /// <param name="options">Import options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count of successfully added containers.</returns>
    private static async Task<int> AddDiscoveredPaths(
        SqlConnection connection,
        DataStoreConfiguration dataStore,
        IGenericResult<List<DatabaseObjectInfo>> discoveryResult,
        string containerTypeId,
        Func<SqlConnection, DatabaseObjectInfo, SchemaImporterOptions?, CancellationToken, Task<IGenericResult<DataContainerConfiguration>>> containerCreator,
        SchemaImporterOptions? options,
        CancellationToken cancellationToken)
    {
        if (!discoveryResult.IsSuccess || discoveryResult.Value == null)
            return 0;

        var count = 0;
        foreach (var dbObject in discoveryResult.Value)
        {
            var containerResult = await containerCreator(
                connection,
                dbObject,
                options,
                cancellationToken).ConfigureAwait(false);

            if (containerResult.IsSuccess && containerResult.Value != null)
            {
                var path = new DataPathConfiguration
                {
                    Id = Guid.NewGuid(),
                    Name = dbObject.ObjectName,
                    PathValue = $"{dbObject.SchemaName}.{dbObject.ObjectName}",
                    PathType = "DatabasePath",
                    SourceDescription = null
                };
                path.Containers.Add(containerResult.Value);
                dataStore.Paths.Add(path);
                count++;
            }
        }

        return count;
    }

    private static async Task<IGenericResult<List<DatabaseObjectInfo>>> ExecuteDiscoveryQuery(
        SqlConnection connection,
        string query,
        SchemaImporterOptions? options,
        CancellationToken cancellationToken)
    {
        try
        {
            var objects = new List<DatabaseObjectInfo>();

            var command = new SqlCommand(query, connection);
            await using (command.ConfigureAwait(false))
            {
                var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                await using (reader.ConfigureAwait(false))
                {

                    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        var schemaName = reader.GetString(0);
                        var objectName = reader.GetString(1);
                        var objectType = reader.GetString(2);

                        // Apply filters if specified
                        if (options?.IncludeSchemas != null && options.IncludeSchemas.Any())
                        {
                            if (!options.IncludeSchemas.Contains(schemaName, StringComparer.OrdinalIgnoreCase))
                                continue;
                        }

                        if (options?.ExcludeSchemas != null && options.ExcludeSchemas.Any())
                        {
                            if (options.ExcludeSchemas.Contains(schemaName, StringComparer.OrdinalIgnoreCase))
                                continue;
                        }

                        objects.Add(new DatabaseObjectInfo
                        {
                            SchemaName = schemaName,
                            ObjectName = objectName,
                            ObjectType = objectType
                        });
                    }

                    return GenericResult<List<DatabaseObjectInfo>>.Success(objects);
                }
            }
        }
        catch (Exception ex)
        {
            return GenericResult<List<DatabaseObjectInfo>>.Failure(
                SqlServerDataStoreResultCodes.ByName("DiscoveryQueryFailed"),
                ResultDetails.Create("error", ex.Message));
        }
    }

    #endregion

    #region Extended Properties Discovery

    /// <summary>
    /// Discovers extended properties (MS_Description, etc.) for all objects in the database.
    /// </summary>
    /// <param name="connection">The SQL connection.</param>
    /// <param name="options">Import options.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary keyed by schema.object.column (column is empty for table/view level).</returns>
#pragma warning disable MA0051 // Method is too long - SQL query string literal, not complex
    private static async Task<IGenericResult<Dictionary<string, List<ExtendedPropertyInfo>>>> DiscoverExtendedProperties(
        SqlConnection connection,
        SchemaImporterOptions? options,
        ILogger logger,
        CancellationToken cancellationToken)
#pragma warning restore MA0051
    {
        if (options?.IncludeExtendedProperties == false)
        {
            return GenericResult<Dictionary<string, List<ExtendedPropertyInfo>>>.Success(
                new Dictionary<string, List<ExtendedPropertyInfo>>(StringComparer.OrdinalIgnoreCase));
        }

        const string query = @"
            SELECT
                SCHEMA_NAME(o.schema_id) AS SchemaName,
                o.name AS ObjectName,
                COALESCE(c.name, '') AS ColumnName,
                ep.name AS PropertyName,
                CAST(ep.value AS NVARCHAR(MAX)) AS PropertyValue,
                ep.minor_id,
                CASE
                    WHEN ep.minor_id = 0 AND o.type = 'U' THEN 'Table'
                    WHEN ep.minor_id = 0 AND o.type = 'V' THEN 'View'
                    WHEN ep.minor_id = 0 AND o.type IN ('P', 'PC') THEN 'StoredProcedure'
                    WHEN ep.minor_id > 0 THEN 'Column'
                    ELSE 'Unknown'
                END AS TargetType
            FROM sys.extended_properties ep
            JOIN sys.objects o ON ep.major_id = o.object_id
            LEFT JOIN sys.columns c ON ep.minor_id = c.column_id AND ep.major_id = c.object_id
            WHERE ep.class = 1
                AND SCHEMA_NAME(o.schema_id) NOT IN ('sys', 'INFORMATION_SCHEMA')
            ORDER BY o.schema_id, o.name, ep.minor_id, ep.name";

        try
        {
            var properties = new Dictionary<string, List<ExtendedPropertyInfo>>(StringComparer.OrdinalIgnoreCase);

            var command = new SqlCommand(query, connection);
            await using (command.ConfigureAwait(false))
            {
                var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                await using (reader.ConfigureAwait(false))
                {
                    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        var schemaName = reader.GetString(0);
                        var objectName = reader.GetString(1);
                        var columnName = reader.GetString(2);
                        var propertyName = reader.GetString(3);
                        var propertyValue = await reader.IsDBNullAsync(4, cancellationToken).ConfigureAwait(false)
                            ? null
                            : reader.GetString(4);
                        var targetType = reader.GetString(6);

                        // Build lookup key: schema.object or schema.object.column
                        var key = string.IsNullOrEmpty(columnName)
                            ? $"{schemaName}.{objectName}"
                            : $"{schemaName}.{objectName}.{columnName}";

                        if (!properties.TryGetValue(key, out var propList))
                        {
                            propList = [];
                            properties[key] = propList;
                        }

                        propList.Add(new ExtendedPropertyInfo
                        {
                            SchemaName = schemaName,
                            ObjectName = objectName,
                            ColumnName = columnName,
                            PropertyName = propertyName,
                            PropertyValue = propertyValue,
                            TargetType = targetType
                        });
                    }

                    return GenericResult<Dictionary<string, List<ExtendedPropertyInfo>>>.Success(properties);
                }
            }
        }
        catch (Exception ex)
        {
            // Why: extended properties are optional; log the failure and return a non-fatal result
            // so callers can decide whether to treat the absence as an error.
            return GenericResult<Dictionary<string, List<ExtendedPropertyInfo>>>.Failure(
                SqlServerSchemaImporterLogger.ExtendedPropertiesFailed(logger, ex));
        }
    }

    /// <summary>
    /// Gets the MS_Description extended property value for an object or column.
    /// </summary>
    private static string? GetDescription(
        Dictionary<string, List<ExtendedPropertyInfo>>? extendedProperties,
        string schemaName,
        string objectName,
        string? columnName = null)
    {
        if (extendedProperties == null)
            return null;

        var key = string.IsNullOrEmpty(columnName)
            ? $"{schemaName}.{objectName}"
            : $"{schemaName}.{objectName}.{columnName}";

        if (!extendedProperties.TryGetValue(key, out var propList))
            return null;

        var descProp = propList.Find(p =>
            string.Equals(p.PropertyName, "MS_Description", StringComparison.OrdinalIgnoreCase));

        return descProp?.PropertyValue;
    }

    /// <summary>
    /// Gets all extended properties for an object or column.
    /// </summary>
    internal static List<ExtendedPropertyInfo> GetExtendedProperties(
        Dictionary<string, List<ExtendedPropertyInfo>>? extendedProperties,
        string schemaName,
        string objectName,
        string? columnName = null)
    {
        if (extendedProperties == null)
            return [];

        var key = string.IsNullOrEmpty(columnName)
            ? $"{schemaName}.{objectName}"
            : $"{schemaName}.{objectName}.{columnName}";

        return extendedProperties.TryGetValue(key, out var propList)
            ? propList
            : [];
    }

    #endregion

    #region Internal Types

    private sealed class DatabaseObjectInfo
    {
        public string SchemaName { get; init; } = string.Empty;
        public string ObjectName { get; init; } = string.Empty;
        public string ObjectType { get; init; } = string.Empty;
    }

    private sealed class ColumnInfo
    {
        public string Name { get; init; } = string.Empty;
        public string DataType { get; init; } = string.Empty;
        public bool IsNullable { get; init; }
        public int? MaxLength { get; init; }
        public byte? Precision { get; init; }
        public int? Scale { get; init; }
        public string? DefaultValue { get; init; }
        public bool IsPrimaryKey { get; init; }
        public bool IsIdentity { get; init; }
        public bool IsComputed { get; init; }
        public string? Description { get; init; }
    }

    /// <summary>
    /// Extended property information from SQL Server.
    /// </summary>
    internal sealed class ExtendedPropertyInfo
    {
        public string SchemaName { get; init; } = string.Empty;
        public string ObjectName { get; init; } = string.Empty;
        public string ColumnName { get; init; } = string.Empty;
        public string PropertyName { get; init; } = string.Empty;
        public string? PropertyValue { get; init; }
        public string TargetType { get; init; } = string.Empty;
    }

    #endregion
}
