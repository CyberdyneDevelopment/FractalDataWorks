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
using Fdw.Data.Sqlite.Logging;
using Fdw.Data.Sqlite.Results;
using Fdw.Data.Sqlite.Translators;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections.Sqlite;

/// <summary>
/// Translates QueryCommand to a SQLite SELECT statement.
/// </summary>
[TypeOption(typeof(SqliteDataCommandTranslators), "Query", RestrictToCurrentCompilation = true)]
public sealed class SqliteQueryTranslator : SqliteDataCommandTranslatorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteQueryTranslator"/> class.
    /// </summary>
    public SqliteQueryTranslator()
        : base("Query")
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<SqliteCommand>> Translate(
        IDataCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
    {
        if (command is IQueryCommand queryCommand)
            return Translate(queryCommand, container, cancellationToken);

        return Task.FromResult(
            GenericResult<SqliteCommand>.Failure(
                SqliteDataResultCodes.ByName("InvalidCommandType")));
    }

    /// <summary>
    /// Translates an <see cref="IQueryCommand"/> to a SQLite SELECT statement.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Cannot be static — overload resolution pattern requires instance method")]
    [ConventionOverride(MaxCyclomaticComplexity = 25)]
    public Task<IGenericResult<SqliteCommand>> Translate(
        IQueryCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (container == null)
                return Task.FromResult(GenericResult<SqliteCommand>.Failure(SqliteDataResultCodes.ByName("ContainerNull")));

            if (container.Path is not IDatabasePath dbPath)
                return Task.FromResult(GenericResult<SqliteCommand>.Failure(SqliteDataResultCodes.ByName("InvalidContainerPath")));

            if (container is not IDataContainer dataContainer)
                return Task.FromResult(GenericResult<SqliteCommand>.Failure(SqliteDataResultCodes.ByName("ContainerNotDataContainer")));

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
                return Task.FromResult(GenericResult<SqliteCommand>.Failure(SqliteDataResultCodes.ByName("NoFieldsToProject")));
            }

            var dialect = dbPath.Dialect;

            var sqlCommand = command.Joins is { Count: > 0 }
                ? BuildJoinedSelectStatement(container, dbPath, dialect, command.Joins, command.Filter, command.Projection, containerFieldNames)
                : BuildSelectStatement(container, dbPath, dialect, command.Filter, command.Projection, command.Ordering, command.Paging, containerFieldNames);

            return Task.FromResult(GenericResult<SqliteCommand>.Success(sqlCommand));
        }
        catch (Exception ex)
        {
            SqliteConnectionLog.TranslationFailed(TranslatorLogger, ex, "Query", ex.Message);
            return Task.FromResult(
                GenericResult<SqliteCommand>.Failure(
                    SqliteDataResultCodes.ByName("QueryTranslationFailed"),
                    ResultDetails.Create("ErrorMessage", ex.Message)));
        }
    }

#pragma warning disable MA0051
    [ConventionOverride(MaxCyclomaticComplexity = 20)]
    private static SqliteCommand BuildSelectStatement(
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

        var command = CreateCommand(sql.ToString());

        if (filter?.Root != null)
        {
            var whereClause = BuildWhereClause(filter, dialect, (n, v) => AddParameter(command, n, v));
            command.CommandText += $" WHERE {whereClause}";
        }

        if (ordering?.OrderedFields?.Any() == true)
        {
            command.CommandText += $" ORDER BY {BuildOrderByClause(ordering, dialect)}";
        }
        else if (paging != null)
        {
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

        if (paging != null)
            command.CommandText += $" {dialect.BuildPagingClause(paging)}";

        return command;
    }

    [ConventionOverride(MaxCyclomaticComplexity = 20)]
    private static SqliteCommand BuildJoinedSelectStatement(
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
                    $"Invalid JOIN target container name '{join.TargetContainerName}'.",
                    nameof(joins));
            }

            var targetQuoted = dialect.QuoteIdentifier(join.TargetContainerName);
            sql.Append(CultureInfo.InvariantCulture, $" {join.JoinType} JOIN {targetQuoted} ON ");

            var conditions = join.JoinConditions.Select(c =>
            {
                if (!IsValidColumnName(c.LeftField) || !IsValidColumnName(c.RightField))
                {
                    throw new ArgumentException(
                        $"Invalid JOIN condition column ('{c.LeftField}', '{c.RightField}').",
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

    [ConventionOverride(MaxCyclomaticComplexity = 20)]  // Why: multi-source branch + PropertyNames + schema + container fallback = branches over default 15.
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
        // SQLite is schemaless — qualifiers are bare table names (no schema prefix).
        if (projection?.Fields.Any(f => f.SourceContainer != null) == true)
        {
            var cols = new List<string>(projection.Fields.Count);
            foreach (var field in projection.Fields)
            {
                if (!IsValidColumnName(field.PropertyName))
                    throw new ArgumentException($"Invalid projection field name '{field.PropertyName}'.", nameof(projection));

                string col;
                if (field.SourceContainer != null)
                {
                    if (!IsValidColumnName(field.SourceContainer))
                        throw new ArgumentException($"Invalid projection source container '{field.SourceContainer}'.", nameof(projection));
                    col = $"{dialect.QuoteIdentifier(field.SourceContainer)}.{dialect.QuoteIdentifier(field.PropertyName)}";
                }
                else
                {
                    col = Col(field.PropertyName);
                }

                if (field.Alias != null)
                {
                    if (!IsValidColumnName(field.Alias))
                        throw new ArgumentException($"Invalid projection alias '{field.Alias}'.", nameof(projection));
                    col = $"{col} AS {dialect.QuoteIdentifier(field.Alias)}";
                }

                cols.Add(col);
            }
            return $"SELECT {string.Join(", ", cols)}";
        }

        if (projection?.PropertyNames?.Any() == true)
        {
            foreach (var propName in projection.PropertyNames)
            {
                if (!IsValidColumnName(propName))
                    throw new ArgumentException($"Invalid projection field name '{propName}'.", nameof(projection));
            }
            return $"SELECT {string.Join(", ", projection.PropertyNames.Select(Col))}";
        }

        if (container.Schema.GetProjectableFields().Count > 0)
            return $"SELECT {string.Join(", ", container.Schema.GetProjectableFields().Select(f => Col(f.Name)))}";

        if (containerFieldNames is { Count: > 0 })
            return $"SELECT {string.Join(", ", containerFieldNames.Select(Col))}";

        throw new InvalidOperationException($"Cannot build SELECT for container '{container.Name}': no columns available.");
    }
}
