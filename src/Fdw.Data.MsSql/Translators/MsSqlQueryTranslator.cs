using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Conventions;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql.Logging;
using Fdw.Data.MsSql.Results;
using Fdw.Data.MsSql.Translators;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// Translates QueryCommand to T-SQL SELECT statement.
/// </summary>
/// <remarks>
/// <para>
/// Builds T-SQL SELECT statements with:
/// <list type="bullet">
/// <item>SELECT clause - field projection or explicit schema fields</item>
/// <item>FROM clause - from container's physical name</item>
/// <item>WHERE clause - from Filter expression (uses FilterOperator.SqlOperator)</item>
/// <item>ORDER BY clause - from Ordering expression</item>
/// <item>OFFSET/FETCH clause - from Paging expression (SQL Server 2012+)</item>
/// </list>
/// </para>
/// <para>
/// Uses FilterOperator properties to avoid switch statements - each operator knows its SQL representation.
/// </para>
/// </remarks>
[TypeOption(typeof(MsSqlDataCommandTranslators), "Query", RestrictToCurrentCompilation = true)]
public sealed class MsSqlQueryTranslator : MsSqlDataCommandTranslatorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlQueryTranslator"/> class.
    /// </summary>
    public MsSqlQueryTranslator()
        : base("Query")
    {
    }

    /// <summary>
    /// Base Translate - dispatches to typed overload or returns error for invalid command types.
    /// </summary>
    public override Task<IGenericResult<SqlCommand>> Translate(
        IDataCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
    {
        // Runtime dispatch to typed overload
        if (command is IQueryCommand queryCommand)
        {
            return Translate(queryCommand, container, cancellationToken);
        }

        return Task.FromResult(
            GenericResult<SqlCommand>.Failure(
                MsSqlTranslatorLog.InvalidCommandType(
                    NullLogger<MsSqlQueryTranslator>.Instance,
                    "MsSqlQueryTranslator",
                    "IQueryCommand",
                    command.GetType().Name)));
    }

    /// <summary>
    /// Translates an IQueryCommand to a T-SQL SELECT statement.
    /// Overload resolution ensures this method is called for IQueryCommand instances.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Cannot be static - overload resolution pattern requires instance method")]
    [ConventionOverride(MaxCyclomaticComplexity = 25)]  // Why: container/path validation + four metadata reads + projection-empty guard pushes branches over the default 15.
    public Task<IGenericResult<SqlCommand>> Translate(
        IQueryCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (container == null)
            {
                return Task.FromResult(
                    GenericResult<SqlCommand>.Failure(MsSqlDataResultCodes.ByName("ContainerNull")));
            }

            // Validate container path is a database path
            if (container.Path is not IDatabasePath dbPath)
            {
                return Task.FromResult(
                    GenericResult<SqlCommand>.Failure(MsSqlDataResultCodes.ByName("InvalidContainerPath")));
            }

            if (container is not IDataContainer dataContainer)
                return Task.FromResult(GenericResult<SqlCommand>.Failure(
                    MsSqlTranslatorLog.ContainerNotDataContainer(NullLogger<MsSqlQueryTranslator>.Instance, container.Name)));

            List<string>? containerFieldNames = null;
            var schemaFields = container.Schema.Fields;
            if (schemaFields.Count > 0)
            {
                var names = new List<string>(schemaFields.Count);
                for (var fi = 0; fi < schemaFields.Count; fi++)
                    names.Add(schemaFields[fi].Name);
                containerFieldNames = names;
            }

            if (command.Projection?.PropertyNames?.Any() != true
                && container.Schema.Fields.Count == 0
                && (containerFieldNames is null || containerFieldNames.Count == 0))
            {
                return Task.FromResult(
                    GenericResult<SqlCommand>.Failure(
                        MsSqlTranslatorLog.NoFieldsToProject(
                            NullLogger<MsSqlQueryTranslator>.Instance,
                            container.Name)));
            }

            var dialect = dbPath.Dialect;

            SqlCommand sqlCommand;
            if (command.Aggregation is not null)
            {
                if (command.Joins is { Count: > 0 })
                {
                    return Task.FromResult(GenericResult<SqlCommand>.Failure(
                        MsSqlDataResultCodes.ByName("QueryTranslationFailed"),
                        ResultDetails.Create("ErrorMessage", "Aggregation combined with JOINs is not supported by the MsSql query translator.")));
                }

                sqlCommand = BuildAggregateSelectStatement(container, dbPath, dialect, command.Aggregation, command.Filter, command.Ordering);
            }
            else if (command.Joins is { Count: > 0 })
            {
                sqlCommand = BuildJoinedSelectStatement(
                    container, dbPath, dialect, command.Joins, command.Filter, command.Projection, containerFieldNames);
            }
            else
            {
                // No JOIN → the single-table path (bare column names).
                sqlCommand = BuildSelectStatement(
                    container,
                    dbPath,
                    dialect,
                    command.Filter,
                    command.Projection,
                    command.Ordering,
                    command.Paging,
                    containerFieldNames);
            }

            return Task.FromResult(GenericResult<SqlCommand>.Success(sqlCommand));
        }
        catch (Exception ex)
        {
            return Task.FromResult(
                GenericResult<SqlCommand>.Failure(
                    MsSqlDataResultCodes.ByName("QueryTranslationFailed"),
                    ResultDetails.Create("ErrorMessage", ex.Message)));
        }
    }

    /// <summary>
    /// Builds a complete T-SQL SELECT statement.
    /// </summary>
    // MA0051: Method length acceptable - sequential SQL SELECT generation (SELECT, FROM, WHERE, ORDER BY with fallback, OFFSET/FETCH)
#pragma warning disable MA0051 // Method is too long
    [ConventionOverride(MaxCyclomaticComplexity = 20)]  // SQL SELECT builder with conditional clauses (projection, WHERE, ORDER BY fallback logic, paging)
    private static SqlCommand BuildSelectStatement(
        IStorageContainer container,
        IDatabasePath dbPath,
        ISqlDialect dialect,
        IFilterExpression? filter,
        IProjectionExpression? projection,
        IOrderingExpression? ordering,
        IPagingExpression? paging,
        List<string>? containerFieldNames)
#pragma warning restore MA0051
    {
        var selectClause = BuildSelectClause(container, projection, containerFieldNames, null, dialect);
        var fromClause = $"FROM {BuildQualifiedTableName(dbPath)}";

        var sql = new StringBuilder();
        sql.Append(selectClause);
        sql.Append(' ');
        sql.Append(fromClause);

        // Get SqlCommand
        var command = CreateCommand(sql.ToString());

        // WHERE clause
        if (filter?.Root != null)
        {
            var whereClause = BuildWhereClause(filter, dialect, (n, v) => AddParameter(command, n, v));
            command.CommandText += $" WHERE {whereClause}";
        }

        // ORDER BY clause (required for OFFSET/FETCH)
        if (ordering?.OrderedFields?.Any() == true)
        {
            command.CommandText += $" ORDER BY {BuildOrderByClause(ordering, dialect)}";
        }
        else if (paging != null)
        {
            // OFFSET/FETCH requires ORDER BY - use first field or primary key
            var fields = container.Schema.Fields;
            var orderByField = "1";

            if (fields.Count > 0)
            {
                var pkName = ((IDataContainer)container).GetPrimaryKeyFieldName();
                orderByField = pkName ?? fields[0].Name;
            }
            else if (containerFieldNames is { Count: > 0 })
            {
                orderByField = containerFieldNames[0];
            }

            command.CommandText += $" ORDER BY {dialect.QuoteIdentifier(orderByField)}";
        }

        // OFFSET/FETCH clause (paging)
        if (paging != null)
        {
            command.CommandText += $" {dialect.BuildPagingClause(paging)}";
        }

        return command;
    }

    /// <summary>
    /// Builds a JOIN read: the primary (child) container's columns SELECTed and table-qualified,
    /// FROM the child, joined to each target container on the supplied column pairs, with a
    /// table-qualified WHERE. Used by the configuration typed-body read (child joined to its parent
    /// on the FK from metadata, filtered by the parent's durable Id). Never emits <c>SELECT *</c>;
    /// only the primary container's columns are projected (the joined parent is not projected).
    /// </summary>
    [ConventionOverride(MaxCyclomaticComplexity = 20)]  // JOIN iteration + condition validation + WHERE.
    private static SqlCommand BuildJoinedSelectStatement(
        IStorageContainer container,
        IDatabasePath dbPath,
        ISqlDialect dialect,
        IReadOnlyList<IJoinExpression> joins,
        IFilterExpression? filter,
        IProjectionExpression? projection,
        List<string>? containerFieldNames)
    {
        var primaryTable = dbPath.ObjectName;

        var sql = new StringBuilder();
        sql.Append(BuildSelectClause(container, projection, containerFieldNames, primaryTable, dialect));
        sql.Append(" FROM ");
        sql.Append(BuildQualifiedTableName(dbPath));

        foreach (var join in joins)
        {
            if (!IsValidColumnName(join.TargetContainerName))
            {
                throw new ArgumentException(
                    $"Invalid JOIN target container name '{join.TargetContainerName}'. Only alphanumeric characters and underscores allowed.",
                    nameof(joins));
            }

            // Build the join target's qualified name using the same schema as the primary path
            var targetQuoted = string.IsNullOrEmpty(dbPath.Database)
                ? $"{dialect.QuoteIdentifier(dbPath.Schema!)}.{dialect.QuoteIdentifier(join.TargetContainerName)}"
                : $"{dialect.QuoteIdentifier(dbPath.Database)}.{dialect.QuoteIdentifier(dbPath.Schema!)}.{dialect.QuoteIdentifier(join.TargetContainerName)}";

            sql.Append(CultureInfo.InvariantCulture, $" {join.JoinType} JOIN {targetQuoted} ON ");

            var conditions = join.JoinConditions.Select(c =>
            {
                // SQL INJECTION PREVENTION: validate both identifiers. Left = primary (child) column,
                // Right = the join target's column.
                if (!IsValidColumnName(c.LeftField) || !IsValidColumnName(c.RightField))
                {
                    throw new ArgumentException(
                        $"Invalid JOIN condition column ('{c.LeftField}', '{c.RightField}'). Only alphanumeric characters and underscores allowed.",
                        nameof(joins));
                }

                return $"{dialect.QuoteIdentifier(primaryTable)}.{dialect.QuoteIdentifier(c.LeftField)} = {dialect.QuoteIdentifier(join.TargetContainerName)}.{dialect.QuoteIdentifier(c.RightField)}";
            });
            sql.Append(string.Join(" AND ", conditions));
        }

        var command = CreateCommand(sql.ToString());

        // WHERE — bare columns qualify to the primary (child) table; dotted "Parent.Col" qualify to
        // the parent table (how the caller filters on the parent's durable Id).
        if (filter?.Root != null)
        {
            var whereClause = BuildWhereClause(filter, dialect, (n, v) => AddParameter(command, n, v), primaryTable);
            command.CommandText += $" WHERE {whereClause}";
        }

        return command;
    }

    /// <summary>
    /// Builds the SELECT clause for a query — explicit projection, schema fields, or container
    /// field names. Never emits <c>SELECT *</c>. When <paramref name="tableQualifier"/> is set
    /// (a JOIN is present), each column is qualified with that table name.
    /// </summary>
    [ConventionOverride(MaxCyclomaticComplexity = 20)]  // Why: multi-source projection branch + single-source paths + schema/container fallback = branches over default 15.
    private static string BuildSelectClause(
        IStorageContainer container,
        IProjectionExpression? projection,
        List<string>? containerFieldNames,
        string? tableQualifier,
        ISqlDialect dialect)
    {
        string Col(string name) => tableQualifier is null
            ? dialect.QuoteIdentifier(name)
            : $"{dialect.QuoteIdentifier(tableQualifier)}.{dialect.QuoteIdentifier(name)}";

        // Multi-source projection: each field carries its own SourceContainer qualifier.
        // This branch fires for compound (pushed-down JOIN) queries where columns from
        // different containers are projected with per-field qualifiers and logical aliases.
        // Single-source queries never set SourceContainer, so they never enter this branch.
        if (projection?.Fields.Any(f => f.SourceContainer != null) == true)
        {
            var cols = new List<string>(projection.Fields.Count);
            foreach (var field in projection.Fields)
            {
                if (!IsValidColumnName(field.PropertyName))
                    throw new ArgumentException(
                        $"Invalid projection field name '{field.PropertyName}'. Only alphanumeric characters and underscores allowed.",
                        nameof(projection));

                string col;
                if (field.SourceContainer != null)
                {
                    if (!IsValidColumnName(field.SourceContainer))
                        throw new ArgumentException(
                            $"Invalid projection source container '{field.SourceContainer}'. Only alphanumeric characters and underscores allowed.",
                            nameof(projection));
                    col = $"{dialect.QuoteIdentifier(field.SourceContainer)}.{dialect.QuoteIdentifier(field.PropertyName)}";
                }
                else
                {
                    col = Col(field.PropertyName);
                }

                if (field.Alias != null)
                {
                    if (!IsValidColumnName(field.Alias))
                        throw new ArgumentException(
                            $"Invalid projection alias '{field.Alias}'. Only alphanumeric characters and underscores allowed.",
                            nameof(projection));
                    col = $"{col} AS {dialect.QuoteIdentifier(field.Alias)}";
                }

                cols.Add(col);
            }
            return $"SELECT {string.Join(", ", cols)}";
        }

        // Explicit projection — caller's intent wins.
        if (projection?.PropertyNames?.Any() == true)
        {
            foreach (var propName in projection.PropertyNames)
            {
                if (!IsValidColumnName(propName))
                {
                    throw new ArgumentException(
                        $"Invalid projection field name '{propName}'. Only alphanumeric characters and underscores allowed.",
                        nameof(projection));
                }
            }

            return $"SELECT {string.Join(", ", projection.PropertyNames.Select(Col))}";
        }

        // Root container with a populated Schema (TableContainer flow).
        if (container.Schema.GetProjectableFields().Count > 0)
        {
            return $"SELECT {string.Join(", ", container.Schema.GetProjectableFields().Select(f => Col(f.Name)))}";
        }

        // Adapter flow with empty Schema — use container field names from IDataNode.Fields.
        if (containerFieldNames is { Count: > 0 })
        {
            return $"SELECT {string.Join(", ", containerFieldNames.Select(Col))}";
        }

        throw new InvalidOperationException(
            $"Cannot build SELECT for container '{container.Name}': no columns available.");
    }

    /// <summary>
    /// SQL-aggregate function keywords this translator will emit. The producer (a DataSet strategy
    /// building an <see cref="IAggregationExpression"/> from <c>DataSetConfiguration.Aggregates</c>) is
    /// responsible for mapping domain function names to one of these; anything else fails loud here.
    /// </summary>
    private static readonly HashSet<string> AllowedAggregateFunctions =
        new(StringComparer.OrdinalIgnoreCase) { "SUM", "COUNT", "AVG", "MIN", "MAX" };

    /// <summary>
    /// Builds a query-time aggregate SELECT: the group-by columns plus each aggregate expression
    /// (<c>FUNC(col) AS alias</c>), a WHERE from the filter, and a GROUP BY over the group-by columns.
    /// Never emits <c>SELECT *</c>; every identifier is validated and quoted.
    /// </summary>
    [ConventionOverride(MaxCyclomaticComplexity = 20)]  // Why: group-by + aggregate projection loops + WHERE + optional ORDER BY.
    private static SqlCommand BuildAggregateSelectStatement(
        IStorageContainer container,
        IDatabasePath dbPath,
        ISqlDialect dialect,
        IAggregationExpression aggregation,
        IFilterExpression? filter,
        IOrderingExpression? ordering)
    {
        if (aggregation.GroupByFields.Count == 0 && aggregation.Aggregations.Count == 0)
        {
            throw new InvalidOperationException(
                $"Aggregation for container '{container.Name}' has neither group-by fields nor aggregate functions.");
        }

        var selectCols = new List<string>(aggregation.GroupByFields.Count + aggregation.Aggregations.Count);

        foreach (var groupField in aggregation.GroupByFields)
        {
            if (!IsValidColumnName(groupField))
            {
                throw new ArgumentException(
                    $"Invalid group-by field '{groupField}'. Only alphanumeric characters and underscores allowed.",
                    nameof(aggregation));
            }

            selectCols.Add(dialect.QuoteIdentifier(groupField));
        }

        foreach (var measure in aggregation.Aggregations)
        {
            if (!IsValidColumnName(measure.Key))
            {
                throw new ArgumentException(
                    $"Invalid aggregate output column '{measure.Key}'. Only alphanumeric characters and underscores allowed.",
                    nameof(aggregation));
            }

            selectCols.Add($"{BuildAggregateExpression(measure.Value, dialect)} AS {dialect.QuoteIdentifier(measure.Key)}");
        }

        var sql = new StringBuilder();
        sql.Append("SELECT ");
        sql.Append(string.Join(", ", selectCols));
        sql.Append(" FROM ");
        sql.Append(BuildQualifiedTableName(dbPath));

        var command = CreateCommand(sql.ToString());

        // WHERE applies before grouping.
        if (filter?.Root != null)
        {
            var whereClause = BuildWhereClause(filter, dialect, (n, v) => AddParameter(command, n, v));
            command.CommandText += $" WHERE {whereClause}";
        }

        if (aggregation.GroupByFields.Count > 0)
        {
            command.CommandText += $" GROUP BY {string.Join(", ", aggregation.GroupByFields.Select(dialect.QuoteIdentifier))}";
        }

        if (ordering?.OrderedFields?.Any() == true
            && ordering.OrderedFields.All(o => aggregation.GroupByFields.Contains(o.PropertyName, StringComparer.OrdinalIgnoreCase)))
        {
            command.CommandText += $" ORDER BY {BuildOrderByClause(ordering, dialect)}";
        }

        return command;
    }

    /// <summary>
    /// Parses a producer-supplied aggregate spec of the form <c>FUNC(Field)</c> or <c>FUNC(*)</c> and
    /// re-emits it with the function validated against <see cref="AllowedAggregateFunctions"/> and the
    /// inner column quoted. Rejecting anything outside the grammar keeps the path injection-safe even
    /// though the spec value is a string.
    /// </summary>
    private static string BuildAggregateExpression(string spec, ISqlDialect dialect)
    {
        var open = spec.IndexOf('(', StringComparison.Ordinal);
        var close = spec.LastIndexOf(')');
        if (open <= 0 || close != spec.Length - 1 || close <= open)
        {
            throw new ArgumentException($"Invalid aggregate spec '{spec}'. Expected FUNC(Field) or FUNC(*).", nameof(spec));
        }

        var func = spec.Substring(0, open).Trim();
        if (!AllowedAggregateFunctions.Contains(func))
        {
            throw new ArgumentException(
                $"Unsupported aggregate function '{func}' in spec '{spec}'. Allowed: {string.Join(", ", AllowedAggregateFunctions)}.",
                nameof(spec));
        }

        var inner = spec.Substring(open + 1, close - open - 1).Trim();
        if (string.Equals(inner, "*", StringComparison.Ordinal))
        {
            return $"{func.ToUpperInvariant()}(*)";
        }

        if (!IsValidColumnName(inner))
        {
            throw new ArgumentException($"Invalid aggregate column '{inner}' in spec '{spec}'.", nameof(spec));
        }

        return $"{func.ToUpperInvariant()}({dialect.QuoteIdentifier(inner)})";
    }
}
