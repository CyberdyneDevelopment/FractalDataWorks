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
using Fdw.Data.PostgreSql.Logging;
using Fdw.Data.PostgreSql.Results;
using Fdw.Data.PostgreSql.Translators;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// Translates QueryCommand to PostgreSQL SELECT statement.
/// </summary>
/// <remarks>
/// <para>
/// Builds PostgreSQL SELECT statements with:
/// <list type="bullet">
/// <item>SELECT clause - field projection or SELECT *</item>
/// <item>FROM clause - from container's physical name with double-quoted identifiers</item>
/// <item>WHERE clause - from Filter expression</item>
/// <item>ORDER BY clause - from Ordering expression</item>
/// <item>LIMIT/OFFSET clause - from Paging expression (PostgreSQL native syntax)</item>
/// </list>
/// </para>
/// </remarks>
[TypeOption(typeof(PostgreSqlDataCommandTranslators), "Query", RestrictToCurrentCompilation = true)]
public sealed class PostgreSqlQueryTranslator : PostgreSqlDataCommandTranslatorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlQueryTranslator"/> class.
    /// </summary>
    public PostgreSqlQueryTranslator()
        : base("Query")
    {
    }

    /// <summary>
    /// Base Translate - dispatches to typed overload or returns error for invalid command types.
    /// </summary>
    public override Task<IGenericResult<NpgsqlCommand>> Translate(
        IDataCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
    {
        if (command is IQueryCommand queryCommand)
        {
            return Translate(queryCommand, container, cancellationToken);
        }

        return Task.FromResult(
            GenericResult<NpgsqlCommand>.Failure(
                PostgreSqlTranslatorLog.InvalidCommandType(
                    NullLogger<PostgreSqlQueryTranslator>.Instance,
                    "PostgreSqlQueryTranslator",
                    "IQueryCommand",
                    command.GetType().Name)));
    }

    /// <summary>
    /// Translates an IQueryCommand to a PostgreSQL SELECT statement.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Cannot be static - overload resolution pattern requires instance method")]
    [ConventionOverride(MaxCyclomaticComplexity = 20)]  // Why: container/path validation + JOIN dispatch + projection-empty guard pushes branches over the default 15.
    public Task<IGenericResult<NpgsqlCommand>> Translate(
        IQueryCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (container == null)
            {
                return Task.FromResult(
                    GenericResult<NpgsqlCommand>.Failure(PostgreSqlDataResultCodes.ByName("ContainerNull")));
            }

            if (container.Path is not IDatabasePath dbPath)
            {
                return Task.FromResult(
                    GenericResult<NpgsqlCommand>.Failure(PostgreSqlDataResultCodes.ByName("InvalidContainerPath")));
            }

            if (command.Projection?.PropertyNames?.Any() != true
                && command.Projection?.Fields.Any(f => f.SourceContainer != null) != true
                && container.Schema.Fields.Count == 0
                && command.Joins is not { Count: > 0 })
            {
                return Task.FromResult(
                    GenericResult<NpgsqlCommand>.Failure(PostgreSqlDataResultCodes.ByName("NoFieldsToProject")));
            }

            var npgsqlCommand = command.Joins is { Count: > 0 }
                ? BuildJoinedSelectStatement(container, dbPath, command.Joins, command.Filter, command.Projection)
                : BuildSelectStatement(container, dbPath, command.Filter, command.Projection, command.Ordering, command.Paging);

            return Task.FromResult(GenericResult<NpgsqlCommand>.Success(npgsqlCommand));
        }
        catch (Exception ex)
        {
            return Task.FromResult(
                GenericResult<NpgsqlCommand>.Failure(
                    PostgreSqlDataResultCodes.ByName("QueryTranslationFailed"),
                    ResultDetails.Create("ErrorMessage", ex.Message)));
        }
    }

    /// <summary>
    /// Builds a complete PostgreSQL SELECT statement.
    /// </summary>
#pragma warning disable MA0051 // Method is too long
    [ConventionOverride(MaxCyclomaticComplexity = 20)]
    private static NpgsqlCommand BuildSelectStatement(
        IStorageContainer container,
        IDatabasePath dbPath,
        IFilterExpression? filter,
        IProjectionExpression? projection,
        IOrderingExpression? ordering,
        IPagingExpression? paging)
#pragma warning restore MA0051
    {
        var dialect = dbPath.Dialect;
        var sql = new StringBuilder();

        sql.Append(BuildSelectClause(container, projection, null, dialect));

        // FROM clause
        sql.Append(CultureInfo.InvariantCulture, $" FROM {BuildQualifiedTableName(dbPath)}");

        // Get NpgsqlCommand
        var command = CreateCommand(sql.ToString());

        // WHERE clause
        if (filter?.Root != null)
        {
            var whereClause = BuildWhereClause(filter, dialect, (n, v) => AddParameter(command, n, v));
            command.CommandText += $" WHERE {whereClause}";
        }

        // ORDER BY clause
        if (ordering?.OrderedFields?.Any() == true)
        {
            var orderByClause = BuildOrderByClause(ordering, dialect);
            command.CommandText += $" ORDER BY {orderByClause}";
        }
        else if (paging != null)
        {
            // PostgreSQL LIMIT/OFFSET does not strictly require ORDER BY,
            // but for deterministic results, add a default ordering
            var fields = container.Schema.Fields;
            var orderByField = "1";

            if (fields.Count > 0)
            {
                var pkName = container.GetPrimaryKeyFieldName();
                orderByField = pkName ?? fields[0].Name;
            }

            command.CommandText += $" ORDER BY {dialect.QuoteIdentifier(orderByField)}";
        }

        // LIMIT/OFFSET clause (paging) - PostgreSQL native syntax
        if (paging != null)
        {
            command.CommandText += $" {dialect.BuildPagingClause(paging)}";
        }

        return command;
    }

    /// <summary>
    /// Builds a JOIN read: columns projected with per-field source qualifiers, FROM the primary
    /// container, joined to each target container on the supplied column pairs. Used by compound
    /// (pushed-down JOIN) datasets where all sources live in a single PostgreSQL store.
    /// Never emits <c>SELECT *</c>.
    /// </summary>
    [ConventionOverride(MaxCyclomaticComplexity = 20)]  // JOIN iteration + condition validation + WHERE.
    private static NpgsqlCommand BuildJoinedSelectStatement(
        IStorageContainer container,
        IDatabasePath dbPath,
        IReadOnlyList<IJoinExpression> joins,
        IFilterExpression? filter,
        IProjectionExpression? projection)
    {
        var primaryTable = dbPath.ObjectName;
        var dialect = dbPath.Dialect;

        var sql = new StringBuilder();
        sql.Append(BuildSelectClause(container, projection, primaryTable, dialect));
        sql.Append(CultureInfo.InvariantCulture, $" FROM {BuildQualifiedTableName(dbPath)}");

        foreach (var join in joins)
        {
            if (!IsValidColumnName(join.TargetContainerName))
            {
                throw new ArgumentException(
                    $"Invalid JOIN target container name '{join.TargetContainerName}'. Only alphanumeric characters and underscores allowed.",
                    nameof(joins));
            }

            var targetQuoted = string.IsNullOrEmpty(dbPath.Schema)
                ? dialect.QuoteIdentifier(join.TargetContainerName)
                : $"{dialect.QuoteIdentifier(dbPath.Schema)}.{dialect.QuoteIdentifier(join.TargetContainerName)}";

            sql.Append(CultureInfo.InvariantCulture, $" {join.JoinType} JOIN {targetQuoted} ON ");

            var conditions = join.JoinConditions.Select(c =>
            {
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

        if (filter?.Root != null)
        {
            var whereClause = BuildWhereClause(filter, dialect, (n, v) => AddParameter(command, n, v), primaryTable);
            command.CommandText += $" WHERE {whereClause}";
        }

        return command;
    }

    /// <summary>
    /// Builds the SELECT clause — multi-source qualified projection, explicit PropertyNames, or
    /// schema fields. Never emits <c>SELECT *</c>.
    /// </summary>
    [ConventionOverride(MaxCyclomaticComplexity = 20)]  // Why: multi-source branch + PropertyNames + schema fields + container field names = branches over default 15.
    private static string BuildSelectClause(
        IStorageContainer container,
        IProjectionExpression? projection,
        string? tableQualifier,
        ISqlDialect dialect)
    {
        string Col(string name) => tableQualifier is null
            ? dialect.QuoteIdentifier(name)
            : $"{dialect.QuoteIdentifier(tableQualifier)}.{dialect.QuoteIdentifier(name)}";

        // Multi-source projection: each field carries its own SourceContainer qualifier.
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
                    throw new ArgumentException(
                        $"Invalid projection field name '{propName}'. Only alphanumeric characters and underscores allowed.",
                        nameof(projection));
            }
            return $"SELECT {string.Join(", ", projection.PropertyNames.Select(Col))}";
        }

        // Schema-driven — root container with a populated Schema.
        if (container.Schema.GetProjectableFields().Count > 0)
            return $"SELECT {string.Join(", ", container.Schema.GetProjectableFields().Select(f => Col(f.Name)))}";

        throw new InvalidOperationException(
            $"Cannot build SELECT for container '{container.Name}': no columns available.");
    }
}
