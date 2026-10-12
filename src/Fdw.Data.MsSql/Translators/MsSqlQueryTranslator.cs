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
                    GenericResult<SqlCommand>.Failure(
                        MsSqlTranslatorLog.QueryContainerNull(NullLogger<MsSqlQueryTranslator>.Instance)));
            }

            // Validate container path is a database path
            if (container.Path is not IDatabasePath dbPath)
            {
                return Task.FromResult(
                    GenericResult<SqlCommand>.Failure(
                        MsSqlTranslatorLog.QueryPathNotDatabase(
                            NullLogger<MsSqlQueryTranslator>.Instance, container.Name, container.Path.GetType().Name)));
            }

            if (container is not IDataContainer dataContainer)
                return Task.FromResult(GenericResult<SqlCommand>.Failure(
                    MsSqlTranslatorLog.ContainerNotDataContainer(NullLogger<MsSqlQueryTranslator>.Instance, container.Name)));

            var returnedFieldNames = ReturnedFieldNames(command, container);

            // Why no fall-back to every declared column: a container whose fields are all withheld
            // (Metadata, Descriptive, DbKey the command did not include) has nothing to return, and
            // selecting everything instead would hand back exactly what the datum types withhold.
            if (command.Projection?.PropertyNames?.Any() != true
                && (returnedFieldNames is null || returnedFieldNames.Count == 0))
            {
                return Task.FromResult(
                    GenericResult<SqlCommand>.Failure(
                        MsSqlTranslatorLog.NoFieldsToProject(
                            NullLogger<MsSqlQueryTranslator>.Instance,
                            container.Name)));
            }

            var dialect = dbPath.Dialect;

            MsSqlTranslatorLog.TranslatingQuery(
                NullLogger<MsSqlQueryTranslator>.Instance,
                container.Name,
                command.Aggregation is not null ? "aggregate" : command.Joins is { Count: > 0 } ? "joined" : "single-table",
                command.Joins.Count);

            return Task.FromResult(BuildStatement(command, dataContainer, dbPath, dialect, returnedFieldNames));
        }
        catch (Exception ex)
        {
            return Task.FromResult(
                GenericResult<SqlCommand>.Failure(
                    MsSqlTranslatorLog.QueryTranslationThrew(
                        NullLogger<MsSqlQueryTranslator>.Instance, ex, container.Name, ex.Message)));
        }
    }

    /// <summary>
    /// The SELECT for the query's shape: an aggregate over one container, a joined read, or a
    /// single-table read.
    /// </summary>
    private static IGenericResult<SqlCommand> BuildStatement(
        IQueryCommand command,
        IDataContainer container,
        IDatabasePath dbPath,
        ISqlDialect dialect,
        List<string>? returnedFieldNames)
    {
        SqlCommand sqlCommand;
        if (command.Aggregation is not null)
        {
            if (command.Joins is { Count: > 0 })
            {
                return GenericResult<SqlCommand>.Failure(
                    MsSqlTranslatorLog.AggregationWithJoins(
                        NullLogger<MsSqlQueryTranslator>.Instance, container.Name, command.Joins.Count));
            }

            sqlCommand = BuildAggregateSelectStatement(
                container, dbPath, dialect, command.Aggregation, command.Filter, command.Ordering, command.Paging);
        }
        else if (command.Joins is { Count: > 0 })
        {
            sqlCommand = BuildJoinedSelectStatement(
                container, dbPath, dialect, command.Joins, command.Filter, command.Projection, returnedFieldNames);
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
                returnedFieldNames);
        }

        return GenericResult<SqlCommand>.Success(sqlCommand);
    }

    /// <summary>
    /// The names of the container's fields this command returns, or null when the container declares
    /// no fields. Each field's datum type decides, given what the command includes (FDW-988).
    /// </summary>
    private static List<string>? ReturnedFieldNames(IQueryCommand command, IStorageContainer container)
    {
        if (container.Schema.Fields.Count == 0)
            return null;

        return container.Schema
            .GetReturnedFields(command.IncludeMetadata, command.IncludeDescriptions, command.IncludeKeys)
            .Select(f => f.Name)
            .ToList();
    }

    /// <summary>
    /// Builds a complete T-SQL SELECT statement.
    /// </summary>
    [ConventionOverride(MaxCyclomaticComplexity = 20)]  // SQL SELECT builder with conditional clauses (projection, WHERE, ORDER BY fallback logic, paging)
    private static SqlCommand BuildSelectStatement(
        IStorageContainer container,
        IDatabasePath dbPath,
        ISqlDialect dialect,
        IFilterExpression? filter,
        IProjectionExpression? projection,
        IOrderingExpression? ordering,
        IPagingExpression? paging,
        List<string>? returnedFieldNames)
    {
        var selectClause = BuildSelectClause(container, projection, returnedFieldNames, null, dialect);
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
            else if (returnedFieldNames is { Count: > 0 })
            {
                orderByField = returnedFieldNames[0];
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

    /// <summary>The database path of a JOIN target, found by name among the containers of the primary container's data store.</summary>
    /// <remarks>Container names are unique within a data store; none, or more than one, refuses the join.</remarks>
    private static IDatabasePath JoinTargetPath(IDataContainer primary, string targetContainerName)
    {
        var store = primary.Parent.Store;
        var matches = store.Paths
            .SelectMany(p => p.Containers)
            .Where(c => string.Equals(c.Name, targetContainerName, StringComparison.Ordinal))
            .ToList();
        if (matches.Count != 1)
            throw new ArgumentException(
                $"JOIN target container '{targetContainerName}' is declared {matches.Count} times in data store '{store.Name}'; it must be declared exactly once.",
                nameof(targetContainerName));
        if (matches[0].Path is not IDatabasePath path)
            throw new ArgumentException(
                $"JOIN target container '{targetContainerName}' is not in a database path.", nameof(targetContainerName));
        return path;
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
        IDataContainer container,
        IDatabasePath dbPath,
        ISqlDialect dialect,
        IReadOnlyList<IJoinExpression> joins,
        IFilterExpression? filter,
        IProjectionExpression? projection,
        List<string>? returnedFieldNames)
    {
        var primaryTable = dbPath.ObjectName;

        var sql = new StringBuilder();
        sql.Append(BuildSelectClause(container, projection, returnedFieldNames, primaryTable, dialect));
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

            // The join target is qualified with its own path's schema: a child and its owner can be
            // declared in different paths (transform.FieldMappingTransformation hangs from
            // data.DataSetFieldMapping), so the primary path's schema is not the target's.
            var targetPath = JoinTargetPath(container, join.TargetContainerName);
            var targetQuoted = string.IsNullOrEmpty(targetPath.Database)
                ? $"{dialect.QuoteIdentifier(targetPath.Schema!)}.{dialect.QuoteIdentifier(join.TargetContainerName)}"
                : $"{dialect.QuoteIdentifier(targetPath.Database)}.{dialect.QuoteIdentifier(targetPath.Schema!)}.{dialect.QuoteIdentifier(join.TargetContainerName)}";

            sql.Append(CultureInfo.InvariantCulture, $" {join.JoinType} JOIN {targetQuoted} ON ");

            // The left side belongs to the primary container unless the join continues from an
            // earlier join's target.
            if (join.SourceContainerName is { } source && !IsValidColumnName(source))
            {
                throw new ArgumentException(
                    $"Invalid JOIN source container name '{source}'. Only alphanumeric characters and underscores allowed.",
                    nameof(joins));
            }

            var leftTable = join.SourceContainerName is { } joinedSource ? joinedSource : primaryTable;

            var conditions = join.JoinConditions.Select(c =>
            {
                // SQL INJECTION PREVENTION: validate both identifiers. Left = the source container's
                // column, Right = the join target's column.
                if (!IsValidColumnName(c.LeftField) || !IsValidColumnName(c.RightField))
                {
                    throw new ArgumentException(
                        $"Invalid JOIN condition column ('{c.LeftField}', '{c.RightField}'). Only alphanumeric characters and underscores allowed.",
                        nameof(joins));
                }

                return $"{dialect.QuoteIdentifier(leftTable)}.{dialect.QuoteIdentifier(c.LeftField)} = {dialect.QuoteIdentifier(join.TargetContainerName)}.{dialect.QuoteIdentifier(c.RightField)}";
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
        List<string>? returnedFieldNames,
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

        // The fields the command returns, decided by each field's datum type (FDW-988).
        if (returnedFieldNames is { Count: > 0 })
        {
            return $"SELECT {string.Join(", ", returnedFieldNames.Select(Col))}";
        }

        throw new InvalidOperationException(
            $"Cannot build SELECT for container '{container.Name}': no columns available.");
    }

    /// <summary>
    /// SQL-aggregate function keywords this translator will emit. The producer (a DataSet strategy
    /// building an <see cref="IAggregationExpression"/> from <c>ISimpleDataSetConfiguration.Aggregates</c>) is
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
        IOrderingExpression? ordering,
        IPagingExpression? paging)
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

        // The window runs after GROUP BY and before OFFSET/FETCH, so it counts every group, not the page.
        if (aggregation.IncludeGroupCount && aggregation.GroupByFields.Count > 0)
            selectCols.Add($"COUNT(*) OVER () AS {dialect.QuoteIdentifier(AggregationExpression.GroupCountColumn)}");

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

        AppendAggregateOrderAndPaging(command, dialect, aggregation, ordering, paging);

        return command;
    }

    /// <summary>
    /// Orders and pages a grouped read. An ordering may name a group-by column or an aggregate's output
    /// column; anything else is left out, as it always was. A page needs an order that never ties, so the
    /// group-by columns follow whatever the caller asked for.
    /// </summary>
    private static void AppendAggregateOrderAndPaging(
        SqlCommand command,
        ISqlDialect dialect,
        IAggregationExpression aggregation,
        IOrderingExpression? ordering,
        IPagingExpression? paging)
    {
        var orderable = aggregation.GroupByFields.Concat(aggregation.Aggregations.Keys).ToList();
        var callerOrder = ordering?.OrderedFields is { } fields
            && fields.Any()
            && fields.All(o => orderable.Contains(o.PropertyName, StringComparer.OrdinalIgnoreCase));
        var pages = paging is not null && aggregation.GroupByFields.Count > 0;
        if (!callerOrder && !pages)
            return;

        var clauses = new List<string>();
        if (callerOrder)
            clauses.Add(BuildOrderByClause(ordering!, dialect));

        if (pages)
        {
            var named = ordering?.OrderedFields.Select(o => o.PropertyName).ToList() ?? [];
            clauses.AddRange(aggregation.GroupByFields
                .Where(g => !callerOrder || !named.Contains(g, StringComparer.OrdinalIgnoreCase))
                .Select(dialect.QuoteIdentifier));
        }

        command.CommandText += $" ORDER BY {string.Join(", ", clauses)}";
        if (pages)
            command.CommandText += $" {dialect.BuildPagingClause(paging!)}";
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
