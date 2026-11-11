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
using Fdw.Services.Connections.Sql;

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

            // The fields this command returns: each field's datum type decides, given what the command
            // includes (FDW-988).
            var returnedFieldNames = container.Schema
                .GetReturnedFields(command.IncludeMetadata, command.IncludeDescriptions, command.IncludeKeys)
                .Select(f => f.Name)
                .ToList();

            if (command.Projection?.PropertyNames?.Any() != true
                && command.Projection?.Fields.Any(f => f.SourceContainer != null) != true
                && returnedFieldNames.Count == 0
                && command.Joins is not { Count: > 0 })
            {
                return Task.FromResult(
                    GenericResult<NpgsqlCommand>.Failure(PostgreSqlDataResultCodes.ByName("NoFieldsToProject")));
            }

            if (command.Joins is { Count: > 0 })
            {
                return Task.FromResult(BuildJoinedSelectStatement(container, dbPath, command, returnedFieldNames));
            }

            return Task.FromResult(
                BuildSelectStatement(container, dbPath, command.Filter, command.Projection, command.Ordering, command.Paging, returnedFieldNames));
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
    /// Builds a complete PostgreSQL SELECT statement, or refuses a paged read that has no order to page by.
    /// </summary>
    [ConventionOverride(MaxCyclomaticComplexity = 20)]
    private static IGenericResult<NpgsqlCommand> BuildSelectStatement(
        IStorageContainer container,
        IDatabasePath dbPath,
        IFilterExpression? filter,
        IProjectionExpression? projection,
        IOrderingExpression? ordering,
        IPagingExpression? paging,
        IReadOnlyList<string> returnedFieldNames)
    {
        var dialect = dbPath.Dialect;
        var sql = new StringBuilder();

        sql.Append(BuildSelectClause(container, projection, returnedFieldNames, null, dialect));

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
            // PostgreSQL LIMIT/OFFSET does not require ORDER BY, but a page of rows in no stated order is not a page. The order is
            // the one the container declares for its rows, its primary key; a container that declares none is refused (FDW-3454).
            var declaredOrder = container.GetDeclaredOrderFieldName();
            if (declaredOrder is null)
            {
                return GenericResult<NpgsqlCommand>.Failure(
                    PostgreSqlTranslatorLog.PagedQueryHasNoOrder(NullLogger<PostgreSqlQueryTranslator>.Instance, container.Name));
            }

            command.CommandText += $" ORDER BY {dialect.QuoteIdentifier(declaredOrder)}";
        }

        // LIMIT/OFFSET clause (paging) - PostgreSQL native syntax
        if (paging != null)
        {
            command.CommandText += $" {dialect.BuildPagingClause(paging)}";
        }

        return GenericResult<NpgsqlCommand>.Success(command);
    }

    /// <summary>
    /// Builds a JOIN read: columns projected with per-field source qualifiers, FROM the primary
    /// container, joined to each target container on the supplied column pairs. Used by compound
    /// (pushed-down JOIN) datasets where all sources live in a single PostgreSQL store.
    /// Never emits <c>SELECT *</c>.
    /// </summary>
    [ConventionOverride(MaxCyclomaticComplexity = 20)]  // JOIN iteration + condition validation + WHERE.
    private static IGenericResult<NpgsqlCommand> BuildJoinedSelectStatement(
        IStorageContainer container,
        IDatabasePath dbPath,
        IQueryCommand query,
        IReadOnlyList<string> returnedFieldNames)
    {
        var joins = query.Joins;
        var filter = query.Filter;
        var primaryTable = dbPath.ObjectName;

        // Every identifier a join names is checked before any of it is written into the statement (SQL injection prevention): one that
        // is not a plain name is a coded refusal of the query, never a throw (FDW-4991).
        if (InvalidJoinIdentifier(joins) is { } invalid)
            return GenericResult<NpgsqlCommand>.Failure(
                PostgreSqlTranslatorLog.JoinIdentifierInvalid(NullLogger<PostgreSqlQueryTranslator>.Instance, container.Name, invalid));

        // Each join is stated by its join type's own SQL keyword, never by the text it carries; one the statement cannot state is a coded
        // refusal of the query (FDW-5099).
        if (JoinRefusal(joins, container.Name) is { } joinRefusal)
            return GenericResult<NpgsqlCommand>.Failure(joinRefusal);

        var dialect = dbPath.Dialect;

        var sql = new StringBuilder();
        sql.Append(BuildSelectClause(container, query.Projection, returnedFieldNames, primaryTable, dialect));
        sql.Append(CultureInfo.InvariantCulture, $" FROM {BuildQualifiedTableName(dbPath)}");

        foreach (var join in joins)
        {
            var targetQuoted = string.IsNullOrEmpty(dbPath.Schema)
                ? dialect.QuoteIdentifier(join.TargetContainerName)
                : $"{dialect.QuoteIdentifier(dbPath.Schema)}.{dialect.QuoteIdentifier(join.TargetContainerName)}";

            // The left column is qualified with the container the join continues from, as on MsSql and Sqlite (FDW-5099).
            sql.Append(JoinClause(join, targetQuoted, primaryTable, dialect));
        }

        var command = CreateCommand(sql.ToString());

        if (filter?.Root != null)
        {
            var whereClause = BuildWhereClause(filter, dialect, (n, v) => AddParameter(command, n, v), primaryTable);
            command.CommandText += $" WHERE {whereClause}";
        }

        // The command's ordering and paging follow the WHERE, qualified the same way; a paged join that states no order is refused (FDW-4991).
        var (refused, orderAndPaging) = BuildJoinedOrderAndPaging(query.Ordering, query.Paging, dialect, primaryTable);
        if (refused)
            return GenericResult<NpgsqlCommand>.Failure(
                PostgreSqlTranslatorLog.PagedQueryHasNoOrder(NullLogger<PostgreSqlQueryTranslator>.Instance, container.Name));

        command.CommandText += orderAndPaging;
        return GenericResult<NpgsqlCommand>.Success(command);
    }

    /// <summary>
    /// Builds the SELECT clause — multi-source qualified projection, explicit PropertyNames, or
    /// schema fields. Never emits <c>SELECT *</c>.
    /// </summary>
    [ConventionOverride(MaxCyclomaticComplexity = 20)]  // Why: multi-source branch + PropertyNames + schema fields + container field names = branches over default 15.
    private static string BuildSelectClause(
        IStorageContainer container,
        IProjectionExpression? projection,
        IReadOnlyList<string> returnedFieldNames,
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

        // The fields the command returns, decided by each field's datum type (FDW-988).
        if (returnedFieldNames.Count > 0)
            return $"SELECT {string.Join(", ", returnedFieldNames.Select(Col))}";

        throw new InvalidOperationException(
            $"Cannot build SELECT for container '{container.Name}': no columns available.");
    }
}
