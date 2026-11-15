using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.Formats;
using Fdw.Data.DataStores.SqlServer.Logging;
using Fdw.Data.DataStores.SqlServer.Models;
using Fdw.Data.MsSql;
using Fdw.Results;
using Fdw.Schema;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.DataNodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.DataStores.SqlServer.Catalog;

/// <summary>
/// Reads a SQL Server catalog (tables, views, procedures, columns, parameters) through an <see cref="IDataConnection"/> with query
/// commands over detached containers, never through SqlClient (FDW-5876). The old importer wrote joined SQL (COLUMNPROPERTY, a derived
/// table for the primary key); a query command states one container and a filter, so each join is several reads put together here, on
/// schema and name together (a table name is only unique within its schema).
/// </summary>
public sealed class SqlServerCatalogReader
{
    private const string InformationSchema = "INFORMATION_SCHEMA";
    private const string SysSchemaName = "sys";
    private const string PrimaryKeyConstraint = "PRIMARY KEY";

    private static readonly string[] TableColumns = ["TABLE_SCHEMA", "TABLE_NAME", "TABLE_TYPE"];
    private static readonly string[] SchemataColumns = ["CATALOG_NAME", "SCHEMA_NAME"];
    private static readonly string[] RoutineColumns = ["ROUTINE_SCHEMA", "ROUTINE_NAME", "ROUTINE_TYPE"];

    private static readonly string[] ColumnColumns =
    [
        "TABLE_SCHEMA", "TABLE_NAME", "ORDINAL_POSITION", "COLUMN_NAME", "DATA_TYPE", "IS_NULLABLE",
        "CHARACTER_MAXIMUM_LENGTH", "NUMERIC_PRECISION", "NUMERIC_SCALE", "COLUMN_DEFAULT",
    ];

    private static readonly string[] ParameterColumns =
    [
        "SPECIFIC_SCHEMA", "SPECIFIC_NAME", "ORDINAL_POSITION", "PARAMETER_MODE", "PARAMETER_NAME", "DATA_TYPE",
        "CHARACTER_MAXIMUM_LENGTH", "NUMERIC_PRECISION", "NUMERIC_SCALE",
    ];

    private static readonly string[] ConstraintColumns = ["CONSTRAINT_NAME", "TABLE_SCHEMA", "TABLE_NAME", "CONSTRAINT_TYPE"];
    private static readonly string[] KeyColumnColumns = ["CONSTRAINT_NAME", "TABLE_SCHEMA", "TABLE_NAME", "COLUMN_NAME", "ORDINAL_POSITION"];
    private static readonly string[] SysSchemaColumns = ["schema_id", "name"];
    private static readonly string[] SysObjectColumns = ["object_id", "name", "schema_id"];
    private static readonly string[] SysColumnColumns = ["object_id", "name", "is_identity", "is_computed"];

    private readonly IDataConnection _connection;
    private readonly ILogger<SqlServerCatalogReader> _logger;

    /// <summary>Initializes a new instance of the <see cref="SqlServerCatalogReader"/> class.</summary>
    /// <param name="connection">The connection the catalog is read through.</param>
    /// <param name="logger">The logger.</param>
    public SqlServerCatalogReader(IDataConnection connection, ILogger<SqlServerCatalogReader>? logger)
    {
        _connection = connection;
        _logger = logger ?? NullLogger<SqlServerCatalogReader>.Instance;
    }

    /// <summary>Reads the name of the database the connection is on, from the catalog name every schema row carries.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The database name, a Missing failure when the catalog lists no schema, or the failure of the read.</returns>
    public async Task<IGenericResult<string>> ReadDatabaseName(CancellationToken cancellationToken = default)
    {
        var rows = await Read<InformationSchemaSchemata>(
            InformationSchema,
            "SCHEMATA",
            SchemataColumns,
            [Equal("SCHEMA_NAME", "dbo")],
            [],
            cancellationToken).ConfigureAwait(false);
        return rows.IsFailure
            ? rows.ToNewResult<string>()
            : rows.Value is { Count: > 0 } found && !string.IsNullOrWhiteSpace(found[0].CatalogName)
                ? GenericResult<string>.Success(found[0].CatalogName)
                : GenericResult<string>.Failure(SqlServerCatalogLog.DatabaseNameNotInCatalog(_logger));
    }

    /// <summary>Reads the base tables, outside the sys and INFORMATION_SCHEMA schemas, in schema then name order.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tables, or the failure of the read.</returns>
    public Task<IGenericResult<IReadOnlyList<CatalogObject>>> ReadTables(CancellationToken cancellationToken = default)
        => ReadObjects("BASE TABLE", cancellationToken);

    /// <summary>Reads the views, outside the sys and INFORMATION_SCHEMA schemas, in schema then name order.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The views, or the failure of the read.</returns>
    public Task<IGenericResult<IReadOnlyList<CatalogObject>>> ReadViews(CancellationToken cancellationToken = default)
        => ReadObjects("VIEW", cancellationToken);

    /// <summary>Reads the stored procedures, outside the sys schema, in schema then name order.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The procedures, or the failure of the read.</returns>
    public async Task<IGenericResult<IReadOnlyList<CatalogObject>>> ReadProcedures(CancellationToken cancellationToken = default)
    {
        var rows = await Read<InformationSchemaRoutine>(
            InformationSchema,
            "ROUTINES",
            RoutineColumns,
            [Equal("ROUTINE_TYPE", "PROCEDURE"), NotEqual("ROUTINE_SCHEMA", SysSchemaName)],
            ["ROUTINE_SCHEMA", "ROUTINE_NAME"],
            cancellationToken).ConfigureAwait(false);
        return rows.IsFailure
            ? rows.ToNewResult<IReadOnlyList<CatalogObject>>()
            : GenericResult<IReadOnlyList<CatalogObject>>.Success(
                [.. (rows.Value ?? []).Where(r => string.Equals(r.RoutineType, "PROCEDURE", StringComparison.Ordinal)).Select(r => new CatalogObject { SchemaName = r.RoutineSchema, ObjectName = r.RoutineName })]);
    }

    /// <summary>Reads one table's or view's columns in ordinal order, with primary key, identity and computed flags.</summary>
    /// <param name="schemaName">The schema name.</param>
    /// <param name="objectName">The table or view name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The columns, or the failure of any read it needed.</returns>
    public async Task<IGenericResult<IReadOnlyList<CatalogColumn>>> ReadColumns(string schemaName, string objectName, CancellationToken cancellationToken = default)
    {
        var columns = await Read<InformationSchemaColumn>(
            InformationSchema,
            "COLUMNS",
            ColumnColumns,
            [Equal("TABLE_SCHEMA", schemaName), Equal("TABLE_NAME", objectName)],
            ["ORDINAL_POSITION"],
            cancellationToken).ConfigureAwait(false);
        if (columns.IsFailure)
        {
            return columns.ToNewResult<IReadOnlyList<CatalogColumn>>();
        }

        var primaryKey = await PrimaryKeyColumnNames(schemaName, objectName, cancellationToken).ConfigureAwait(false);
        if (primaryKey.IsFailure)
        {
            return primaryKey.ToNewResult<IReadOnlyList<CatalogColumn>>();
        }

        var flags = await SysColumnFlags(schemaName, objectName, cancellationToken).ConfigureAwait(false);
        if (flags.IsFailure)
        {
            return flags.ToNewResult<IReadOnlyList<CatalogColumn>>();
        }

        return GenericResult<IReadOnlyList<CatalogColumn>>.Success(
        [
            .. (columns.Value ?? [])
                .Where(c => string.Equals(c.TableSchema, schemaName, StringComparison.Ordinal)
                    && string.Equals(c.TableName, objectName, StringComparison.Ordinal))
                .OrderBy(c => c.OrdinalPosition)
                .Select(c => new CatalogColumn
                {
                    Name = c.ColumnName,
                    DataType = c.DataType,
                    IsNullable = string.Equals(c.IsNullable, "YES", StringComparison.Ordinal),
                    MaxLength = c.CharacterMaximumLength,
                    Precision = c.NumericPrecision,
                    Scale = c.NumericScale,
                    DefaultValue = c.ColumnDefault,
                    IsPrimaryKey = (primaryKey.Value ?? []).Contains(c.ColumnName, StringComparer.Ordinal),
                    IsIdentity = (flags.Value ?? []).Any(f => string.Equals(f.Name, c.ColumnName, StringComparison.Ordinal) && f.IsIdentity),
                    IsComputed = (flags.Value ?? []).Any(f => string.Equals(f.Name, c.ColumnName, StringComparison.Ordinal) && f.IsComputed),
                }),
        ]);
    }

    /// <summary>Reads one procedure's parameters in ordinal order.</summary>
    /// <param name="schemaName">The schema name.</param>
    /// <param name="procedureName">The procedure name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The parameters, or the failure of the read.</returns>
    public async Task<IGenericResult<IReadOnlyList<CatalogColumn>>> ReadParameters(string schemaName, string procedureName, CancellationToken cancellationToken = default)
    {
        var rows = await Read<InformationSchemaParameter>(
            InformationSchema,
            "PARAMETERS",
            ParameterColumns,
            [Equal("SPECIFIC_SCHEMA", schemaName), Equal("SPECIFIC_NAME", procedureName)],
            ["ORDINAL_POSITION"],
            cancellationToken).ConfigureAwait(false);
        return rows.IsFailure
            ? rows.ToNewResult<IReadOnlyList<CatalogColumn>>()
            : GenericResult<IReadOnlyList<CatalogColumn>>.Success(
            [
                .. (rows.Value ?? [])
                    .Where(p => string.Equals(p.SpecificSchema, schemaName, StringComparison.Ordinal)
                        && string.Equals(p.SpecificName, procedureName, StringComparison.Ordinal))
                    .OrderBy(p => p.OrdinalPosition)
                    .Select(p => new CatalogColumn
                    {
                        Name = p.ParameterName,
                        DataType = p.DataType,
                        // Why: a parameter is nullable when it is an input or states no mode, as the old query's CASE had it.
                        IsNullable = p.ParameterMode is null || string.Equals(p.ParameterMode, "IN", StringComparison.Ordinal),
                        MaxLength = p.CharacterMaximumLength,
                        Precision = p.NumericPrecision,
                        Scale = p.NumericScale,
                    }),
            ]);
    }

    private static FilterCondition Equal(string column, object value)
        => new FilterCondition { PropertyName = column, Operator = new EqualOperator(), Value = value };

    private static FilterCondition NotEqual(string column, object value)
        => new FilterCondition { PropertyName = column, Operator = new NotEqualOperator(), Value = value };

    private async Task<IGenericResult<IReadOnlyList<CatalogObject>>> ReadObjects(string tableType, CancellationToken cancellationToken)
    {
        var rows = await Read<InformationSchemaTable>(
            InformationSchema,
            "TABLES",
            TableColumns,
            [Equal("TABLE_TYPE", tableType), NotEqual("TABLE_SCHEMA", SysSchemaName), NotEqual("TABLE_SCHEMA", InformationSchema)],
            ["TABLE_SCHEMA", "TABLE_NAME"],
            cancellationToken).ConfigureAwait(false);
        return rows.IsFailure
            ? rows.ToNewResult<IReadOnlyList<CatalogObject>>()
            : GenericResult<IReadOnlyList<CatalogObject>>.Success(
                [.. (rows.Value ?? []).Where(r => string.Equals(r.TableType, tableType, StringComparison.Ordinal)).Select(r => new CatalogObject { SchemaName = r.TableSchema, ObjectName = r.TableName })]);
    }

    // The columns of the table's PRIMARY KEY: the key columns whose constraint is a PRIMARY KEY constraint of that table. A UNIQUE key
    // is not one, and the same table name in another schema is not this table.
    private async Task<IGenericResult<IReadOnlyList<string>>> PrimaryKeyColumnNames(string schemaName, string objectName, CancellationToken cancellationToken)
    {
        var constraints = await Read<InformationSchemaTableConstraint>(
            InformationSchema,
            "TABLE_CONSTRAINTS",
            ConstraintColumns,
            [Equal("TABLE_SCHEMA", schemaName), Equal("TABLE_NAME", objectName), Equal("CONSTRAINT_TYPE", PrimaryKeyConstraint)],
            [],
            cancellationToken).ConfigureAwait(false);
        if (constraints.IsFailure)
        {
            return constraints.ToNewResult<IReadOnlyList<string>>();
        }

        var keyColumns = await Read<InformationSchemaKeyColumnUsage>(
            InformationSchema,
            "KEY_COLUMN_USAGE",
            KeyColumnColumns,
            [Equal("TABLE_SCHEMA", schemaName), Equal("TABLE_NAME", objectName)],
            [],
            cancellationToken).ConfigureAwait(false);
        if (keyColumns.IsFailure)
        {
            return keyColumns.ToNewResult<IReadOnlyList<string>>();
        }

        var primaryKeyNames = (constraints.Value ?? [])
            .Where(c => string.Equals(c.TableSchema, schemaName, StringComparison.Ordinal)
                && string.Equals(c.TableName, objectName, StringComparison.Ordinal)
                && string.Equals(c.ConstraintType, PrimaryKeyConstraint, StringComparison.Ordinal))
            .Select(c => c.ConstraintName)
            .ToHashSet(StringComparer.Ordinal);
        return GenericResult<IReadOnlyList<string>>.Success(
        [
            .. (keyColumns.Value ?? [])
                .Where(k => string.Equals(k.TableSchema, schemaName, StringComparison.Ordinal)
                    && string.Equals(k.TableName, objectName, StringComparison.Ordinal)
                    && primaryKeyNames.Contains(k.ConstraintName))
                .Select(k => k.ColumnName),
        ]);
    }

    // The identity and computed flags INFORMATION_SCHEMA does not carry, by column name, for one table: the schema's id, then the object
    // of that name in that schema, then that object's columns (the old query asked OBJECT_ID(schema.table)).
    private async Task<IGenericResult<IReadOnlyList<SysColumn>>> SysColumnFlags(string schemaName, string objectName, CancellationToken cancellationToken)
    {
        var schemas = await Read<SysSchema>(
            SysSchemaName, "schemas", SysSchemaColumns, [Equal("name", schemaName)], [], cancellationToken).ConfigureAwait(false);
        if (schemas.IsFailure)
        {
            return schemas.ToNewResult<IReadOnlyList<SysColumn>>();
        }

        var objects = await Read<SysObject>(
            SysSchemaName, "objects", SysObjectColumns, [Equal("name", objectName)], [], cancellationToken).ConfigureAwait(false);
        if (objects.IsFailure)
        {
            return objects.ToNewResult<IReadOnlyList<SysColumn>>();
        }

        var schema = (schemas.Value ?? []).FirstOrDefault(s => string.Equals(s.Name, schemaName, StringComparison.Ordinal));
        var target = schema is null
            ? null
            : (objects.Value ?? []).FirstOrDefault(o => o.SchemaId == schema.SchemaId && string.Equals(o.Name, objectName, StringComparison.Ordinal));
        if (target is null)
        {
            return GenericResult<IReadOnlyList<SysColumn>>.Failure(SqlServerCatalogLog.ObjectNotInCatalog(_logger, schemaName, objectName));
        }

        var sysColumns = await Read<SysColumn>(
            SysSchemaName, "columns", SysColumnColumns, [Equal("object_id", target.ObjectId)], [], cancellationToken).ConfigureAwait(false);
        return sysColumns.IsFailure
            ? sysColumns.ToNewResult<IReadOnlyList<SysColumn>>()
            : GenericResult<IReadOnlyList<SysColumn>>.Success([.. (sysColumns.Value ?? []).Where(c => c.ObjectId == target.ObjectId)]);
    }

    // One read of one catalog container: a detached container named for the catalog view, a query command with the conditions ANDed and
    // the ordering stated, and the connection's own failure passed up unchanged.
    private async Task<IGenericResult<IReadOnlyList<TRow>>> Read<TRow>(
        string schema,
        string containerName,
        IReadOnlyList<string> columns,
        IReadOnlyList<FilterCondition> conditions,
        IReadOnlyList<string> orderBy,
        CancellationToken cancellationToken)
    {
        SqlServerCatalogLog.ReadStarted(_logger, schema, containerName);
        var container = new DataContainer(
            containerName,
            schema,
            [.. columns.Select((c, i) => (IDataField)new DataField(c, null, null, i, true, DatumTypes.NotYetTyped))],
            [],
            GenericResult<IReadOnlyList<ReferencingKeyBinding>>.Success([]),
            new ViewContainerType(),
            new TabularFormatType(),
            new DatabasePath(null, schema, containerName),
            ["Query"],
            new Dictionary<string, object>(StringComparer.Ordinal),
            _logger);
        var command = new QueryCommand<TRow>
        {
            Filter = new FilterExpression
            {
                Root = conditions.Count == 1
                    ? conditions[0]
                    : new FilterGroup { Operator = LogicalOperator.And, Nodes = [.. conditions] },
            },
            Ordering = orderBy.Count == 0
                ? null
                : new OrderingExpression
                {
                    OrderedFields = [.. orderBy.Select(o => new OrderedField { PropertyName = o, Direction = new AscendingDirection() })],
                },
        };
        var result = await _connection.Execute<IEnumerable<TRow>>(command, container, cancellationToken).ConfigureAwait(false);
        return result.IsFailure
            ? result.ToNewResult<IReadOnlyList<TRow>>()
            : GenericResult<IReadOnlyList<TRow>>.Success([.. result.Value ?? []]);
    }
}
