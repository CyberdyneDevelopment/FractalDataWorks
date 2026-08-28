using System;
using System.Collections;
using System.Collections.Generic;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.Sql;

/// <summary>
/// Shared base class for SQL-family data command translators.
/// Provides dialect-parameterized WHERE, ORDER BY, and parameter helpers
/// that work across any SQL dialect (T-SQL, PlPgSql, SQLite).
/// </summary>
/// <typeparam name="TCommand">The ADO.NET command type (e.g., <c>SqlCommand</c>,
/// <c>NpgsqlCommand</c>, <c>SqliteCommand</c>).</typeparam>
/// <remarks>
/// <para>
/// Concrete backends derive from this class and supply backend-specific helpers
/// (e.g., <c>CreateCommand</c> and <c>AddParameter</c>) on their own intermediate base
/// (e.g., <c>MsSqlDataCommandTranslatorBase</c>). All WHERE-clause building, ORDER BY
/// construction, column-name validation, and paging are implemented once here,
/// parameterized by an <see cref="ISqlDialect"/> that is threaded from the
/// <see cref="IDatabasePath"/> at translate-time.
/// </para>
/// <para>
/// <strong>No fallbacks.</strong> If the container's path is not an
/// <see cref="IDatabasePath"/>, the translator returns a fail-loud error result.
/// The dialect is always derived from the path — never defaulted.
/// </para>
/// </remarks>
public abstract class SqlDataCommandTranslatorBase<TCommand> : DataCommandTranslatorBase<TCommand>, IQueryCapability
{
    /// <summary>
    /// Initializes a new instance of <see cref="SqlDataCommandTranslatorBase{TCommand}"/>.
    /// </summary>
    /// <param name="name">The translator name (must match the <c>[TypeOption]</c> attribute value).</param>
    /// <param name="domainName">The domain name for this translator (e.g., "MsSql", "PostgreSql", "Sqlite").</param>
    protected SqlDataCommandTranslatorBase(string name, string domainName)
        : base(name, domainName)
    {
    }

    // -----------------------------------------------------------------------
    // Shared helpers — called by per-command translators
    // -----------------------------------------------------------------------

    /// <summary>
    /// Extracts input data from a data command via <see cref="IDataCommandWithInput"/>.
    /// </summary>
    protected static object? GetCommandData(IDataCommand command)
    {
        if (command is IDataCommandWithInput commandWithInput)
            return commandWithInput.InputData;
        return null;
    }

    /// <summary>
    /// Adds parameters from a POCO object's properties to the command.
    /// Only properties matching the supplied field names are included.
    /// </summary>
    /// <param name="data">The source data object.</param>
    /// <param name="fieldNames">The field names to extract from <paramref name="data"/>.</param>
    /// <param name="addParam">
    /// Delegate that adds a named parameter to the underlying command.
    /// Receives the parameter name WITHOUT the SQL prefix (e.g., <c>FieldName</c>, not <c>@FieldName</c>).
    /// Typically <c>(n, v) => AddParameter(command, n, v)</c>.
    /// </param>
    protected static void AddParametersFromObject(object data, IEnumerable<string> fieldNames, Action<string, object?> addParam)
    {
        var type = data.GetType();
        foreach (var fieldName in fieldNames)
        {
            var property = type.GetProperty(fieldName);
            if (property != null)
                addParam(fieldName, property.GetValue(data));
        }
    }

    /// <summary>
    /// Builds the dialect-correct fully-qualified table name from a database path.
    /// Includes the database segment when present; omits the schema segment when the dialect
    /// does not support a schema namespace (e.g., SQLite).
    /// Examples:
    /// <list type="bullet">
    /// <item>TSql, database present: <c>[Database].[Schema].[Object]</c></item>
    /// <item>TSql, no database: <c>[Schema].[Object]</c></item>
    /// <item>SQLite (schemaless): <c>"Object"</c></item>
    /// </list>
    /// </summary>
    /// <param name="path">The database path providing the three-part name and dialect.</param>
    protected static string BuildQualifiedTableName(IDatabasePath path)
    {
        var dialect = path.Dialect;
        var quotedObject = dialect.QuoteIdentifier(path.ObjectName);

        if (!dialect.SupportsSchemaNamespace || string.IsNullOrEmpty(path.Schema))
            return quotedObject;

        var schemaQualified = $"{dialect.QuoteIdentifier(path.Schema)}.{quotedObject}";

        return string.IsNullOrEmpty(path.Database)
            ? schemaQualified
            : $"{dialect.QuoteIdentifier(path.Database)}.{schemaQualified}";
    }

    /// <summary>
    /// Builds the schema-qualified (no database segment) table name.
    /// Used for operations like bulk-copy destination where the database is implied.
    /// Returns just the quoted object name for schemaless dialects (e.g., SQLite).
    /// </summary>
    /// <param name="path">The database path providing the schema/object name and dialect.</param>
    protected static string BuildSchemaQualifiedTableName(IDatabasePath path)
    {
        var dialect = path.Dialect;
        var quotedObject = dialect.QuoteIdentifier(path.ObjectName);

        if (!dialect.SupportsSchemaNamespace || string.IsNullOrEmpty(path.Schema))
            return quotedObject;

        return $"{dialect.QuoteIdentifier(path.Schema)}.{quotedObject}";
    }

    /// <summary>
    /// Validates that a column/identifier name is safe against SQL injection.
    /// Only alphanumeric characters and underscores are allowed; must start with a letter or underscore.
    /// </summary>
    protected static bool IsValidColumnName(string columnName)
    {
        if (string.IsNullOrWhiteSpace(columnName))
            return false;

        if (!char.IsLetter(columnName[0]) && columnName[0] != '_')
            return false;

        for (int i = 1; i < columnName.Length; i++)
        {
            if (!char.IsLetterOrDigit(columnName[i]) && columnName[i] != '_')
                return false;
        }

        return true;
    }

    /// <summary>
    /// Builds a parameterized SQL WHERE clause from a hierarchical filter expression.
    /// All values are parameterized — zero SQL injection risk. Column names are validated.
    /// </summary>
    /// <param name="filter">The filter expression.</param>
    /// <param name="dialect">The SQL dialect for identifier quoting and predicate constants.</param>
    /// <param name="addParam">
    /// Delegate that adds a named parameter to the underlying command.
    /// Receives the parameter name WITHOUT the SQL prefix (e.g., <c>p0</c>, not <c>@p0</c>).
    /// Typically <c>(n, v) => AddParameter(command, n, v)</c>.
    /// </param>
    /// <param name="primaryTableQualifier">
    /// When non-null (a JOIN is present), unqualified columns are qualified with this table name
    /// and dotted columns (<c>Parent.Col</c>) are qualified with their stated table.
    /// Null preserves single-table behaviour (bare quoted column).
    /// </param>
    /// <param name="parameterPrefix">
    /// The FULL SQL parameter prefix including the driver's marker character
    /// (e.g., <c>@</c> for default, <c>@where_</c> to namespace WHERE parameters
    /// when SET parameters are added with a different prefix). Default is <c>@</c>.
    /// </param>
    /// <returns>
    /// The SQL WHERE clause body (without the <c>WHERE</c> keyword), or
    /// <see cref="string.Empty"/> when the filter is empty.
    /// </returns>
    protected static string BuildWhereClause(
        IFilterExpression filter,
        ISqlDialect dialect,
        Action<string, object?> addParam,
        string? primaryTableQualifier = null,
        string parameterPrefix = "@")
    {
        if (filter?.Root == null)
            return string.Empty;

        var context = new SqlBuildContext
        {
            Dialect = dialect,
            AddParam = addParam,
            ParameterPrefix = parameterPrefix,
            PrimaryQualifier = primaryTableQualifier
        };
        return BuildWhereNode(filter.Root, context);
    }

    /// <summary>
    /// Builds an ORDER BY clause from an ordering expression using the supplied dialect for quoting.
    /// </summary>
    /// <param name="ordering">The ordering expression.</param>
    /// <param name="dialect">The SQL dialect for identifier quoting.</param>
    /// <returns>The ORDER BY body SQL (without the <c>ORDER BY</c> keyword).</returns>
    protected static string BuildOrderByClause(IOrderingExpression ordering, ISqlDialect dialect)
    {
        var clauses = new List<string>();
        foreach (var field in ordering.OrderedFields)
        {
            if (!IsValidColumnName(field.PropertyName))
            {
                throw new ArgumentException(
                    $"Invalid ORDER BY field name '{field.PropertyName}'. Only alphanumeric characters and underscores allowed.",
                    nameof(ordering));
            }

            var direction = field.Direction.IsAscending ? "ASC" : "DESC";
            clauses.Add($"{dialect.QuoteIdentifier(field.PropertyName)} {direction}");
        }

        return string.Join(", ", clauses);
    }

    // -----------------------------------------------------------------------
    // Private helpers — WHERE-clause building internals
    // -----------------------------------------------------------------------

    /// <summary>
    /// State threaded through recursive WHERE-clause building.
    /// </summary>
    private sealed class SqlBuildContext
    {
        public required ISqlDialect Dialect { get; init; }

        public required Action<string, object?> AddParam { get; init; }

        public required string ParameterPrefix { get; init; }

        public int ParameterCounter { get; set; }

        public string? PrimaryQualifier { get; init; }
    }

    private static string QualifyColumn(string propertyName, ISqlDialect dialect, string? primaryQualifier)
    {
        var dot = propertyName.IndexOf('.');
        if (dot >= 0)
        {
            var table = propertyName.Substring(0, dot);
            var column = propertyName.Substring(dot + 1);
            if (!IsValidColumnName(table) || !IsValidColumnName(column))
            {
                throw new ArgumentException(
                    $"Invalid qualified column name '{propertyName}'. Only alphanumeric characters and underscores allowed in each part.",
                    nameof(propertyName));
            }

            return $"{dialect.QuoteIdentifier(table)}.{dialect.QuoteIdentifier(column)}";
        }

        if (!IsValidColumnName(propertyName))
        {
            throw new ArgumentException(
                $"Invalid column name '{propertyName}'. Only alphanumeric characters and underscores allowed.",
                nameof(propertyName));
        }

        return primaryQualifier is null
            ? dialect.QuoteIdentifier(propertyName)
            : $"{dialect.QuoteIdentifier(primaryQualifier)}.{dialect.QuoteIdentifier(propertyName)}";
    }

    private static string BuildWhereNode(IFilterNode node, SqlBuildContext context)
    {
        return node switch
        {
            FilterCondition condition => BuildWhereCondition(condition, context),
            FilterGroup group => BuildWhereGroup(group, context),
            _ => throw new InvalidOperationException($"Unknown filter node type: {node.GetType().Name}")
        };
    }

    private static string BuildWhereCondition(FilterCondition condition, SqlBuildContext context)
    {
        var columnName = QualifyColumn(condition.PropertyName, context.Dialect, context.PrimaryQualifier);

        if (condition.Operator.RequiresValue)
        {
            // Special handling for IN operator: expand IEnumerable into individual parameters
            if (condition.Value is IEnumerable enumerable and not string)
            {
                var conditionIndex = context.ParameterCounter++;
                var paramNames = new List<string>();
                var itemIndex = 0;
                foreach (var item in enumerable)
                {
                    // SQL param name (in SQL text): "@p0_0", "@where_p0_0", etc.
                    var sqlParamName = $"{context.ParameterPrefix}p{conditionIndex}_{itemIndex++}";
                    paramNames.Add(sqlParamName);
                    // AddParam key = SQL name without the leading "@" (or dialect prefix char)
                    context.AddParam(sqlParamName.Substring(1), item);
                }

                if (paramNames.Count == 0)
                {
                    // Empty IN list — produce a condition that always evaluates to false
                    return $"{columnName} {condition.Operator.SqlOperator} (SELECT NULL WHERE {context.Dialect.AlwaysFalsePredicate})";
                }

                return $"{columnName} {condition.Operator.SqlOperator} ({string.Join(", ", paramNames)})";
            }

            // SQL param name (in SQL text)
            var singleSqlParamName = $"{context.ParameterPrefix}p{context.ParameterCounter}";
            // AddParam key = SQL name without the leading "@"
            var singleParamKey = singleSqlParamName.Substring(1);
            context.ParameterCounter++;

            // ZERO SQL INJECTION: Value goes into parameter, NOT concatenated into SQL.
            // Preprocess string values to escape operator-specific metacharacters (e.g., LIKE wildcards).
            object? paramValue = condition.Value is string strValue
                ? condition.Operator.PreprocessSqlValue(strValue)
                : condition.Value;

            context.AddParam(singleParamKey, paramValue);
            return $"{columnName} {condition.Operator.SqlOperator} {singleSqlParamName}";
        }

        return $"{columnName} {condition.Operator.SqlOperator}";
    }

    private static string BuildWhereGroup(FilterGroup group, SqlBuildContext context)
    {
        var clauses = new List<string>();
        foreach (var childNode in group.Nodes)
        {
            var clause = BuildWhereNode(childNode, context);
            if (!string.IsNullOrEmpty(clause))
                clauses.Add(clause);
        }

        if (clauses.Count == 0)
            return string.Empty;

        if (clauses.Count == 1)
            return clauses[0]; // Single condition doesn't need parentheses

        var logicalOp = group.Operator == LogicalOperator.Or ? " OR " : " AND ";
        return $"({string.Join(logicalOp, clauses)})"; // Always wrap groups in parentheses for precedence
    }

    /// <inheritdoc />
    /// <remarks>
    /// SQL expresses all three: the filter becomes WHERE, the ordering ORDER BY, and the page the
    /// dialect's own paging clause — OFFSET/FETCH on SQL Server, LIMIT elsewhere. Every operator in
    /// FilterOperators carries a SqlOperator, so there is no condition this cannot put in a WHERE.
    /// </remarks>
    public virtual bool CanExpressFilter(IFilterExpression filter) => true;

    /// <inheritdoc />
    public virtual bool CanExpressOrdering(IOrderingExpression ordering) => true;

    /// <inheritdoc />
    public virtual bool CanExpressPaging(IPagingExpression paging) => true;
}
