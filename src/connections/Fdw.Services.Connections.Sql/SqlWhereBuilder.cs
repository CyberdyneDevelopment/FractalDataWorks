using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Logging;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Sql.FilterNodes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections.Sql;

/// <summary>
/// Builds the WHERE clause of a filter: the column names it writes are validated, every value is a parameter, and a node is written by the
/// handler named for its <c>FilterNodeKinds</c> kind (<see cref="SqlFilterNodeHandlers"/>).
/// </summary>
internal static class SqlWhereBuilder
{
    internal static bool IsValidColumnName(string columnName) => PlainIdentifiers.IsPlain(columnName);

    internal static string QualifyColumn(string propertyName, ISqlDialect dialect, string? primaryQualifier)
    {
        var dot = propertyName.IndexOf('.', StringComparison.Ordinal);
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

    internal static string BuildNode(IFilterNode node, SqlBuildContext context)
    {
        var kind = FilterNodeKinds.Of(node);
        var handler = ReferenceEquals(kind, FilterNodeKinds.NotFound)
            ? SqlFilterNodeHandlers.NotFound
            : SqlFilterNodeHandlers.ByName(kind.Name);
        return ReferenceEquals(handler, SqlFilterNodeHandlers.NotFound)
            ? throw new InvalidOperationException($"Unknown filter node type: {node.GetType().Name}")
            : handler.Build(node, context);
    }

    internal static string BuildCondition(FilterCondition condition, SqlBuildContext context)
    {
        var columnName = QualifyColumn(condition.PropertyName, context.Dialect, context.PrimaryQualifier);

        if (condition.Operator.IsPatternMatch)
            return BuildPatternCondition(columnName, condition, context);

        if (condition.Operator.RequiresValue)
        {
            // Special handling for IN operator: expand IEnumerable into individual parameters
            // Why byte[] is excluded: it is one binary value compared whole, not a list to expand into
            // one parameter per byte, which produced "= (@p0_0, @p0_1, ...)" and SQL 102 (FDW-889).
            if (condition.Value is IEnumerable enumerable and not string and not byte[])
            {
                var conditionIndex = context.ParameterCounter++;
                var items = enumerable.Cast<object?>().ToList();

                // A key list longer than the connection's setting is carried as one JSON parameter and joined through the dialect's own table form (FDW-4267).
                if (TrySpillKeyList(condition, columnName, items, conditionIndex, context) is { } spilled)
                    return spilled;

                var paramNames = new List<string>();
                var itemIndex = 0;
                foreach (var item in items)
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
            context.AddParam(singleParamKey, condition.Value);
            return $"{columnName} {condition.Operator.SqlOperator} {singleSqlParamName}";
        }

        return $"{columnName} {condition.Operator.SqlOperator}";
    }

    // A key list with more values than the connection's setting goes to a table: one JSON parameter carries it, the dialect's form reads it back as a subquery, and
    // the condition keeps its operator (IN or NOT IN). Not spilled, so the values stay parameters: a list at or below the setting, a column of a joined table (its
    // table is not known here), a dialect with no form, and a list that holds a null (IN and NOT IN read a null as no match, which a table of values would not).
    // The setting comes from the translation's scope, which the connection always opens (FDW-4267); outside one the scope stops the translation.
    private static string? TrySpillKeyList(FilterCondition condition, string columnName, List<object?> items, int conditionIndex, SqlBuildContext context)
    {
        var scope = KeyListSpillScope.Required;
        if (scope.SourceTable is null || items.Count <= scope.Settings.MaxParametersBeforeTempTable)
            return null;

        if (condition.PropertyName.Contains('.', StringComparison.Ordinal) || items.Contains(null) || context.Dialect is not IKeyListSpillDialect spillDialect)
            return null;

        var jsonParameter = $"{context.ParameterPrefix}kl{conditionIndex}";
        context.AddParam(jsonParameter.Substring(1), spillDialect.KeyListJson(condition.PropertyName, items));
        var form = spillDialect.KeyListSpill(Guid.NewGuid().ToString("N"), scope.SourceTable, condition.PropertyName, jsonParameter);
        scope.Add(form);
        return $"{columnName} {condition.Operator.SqlOperator} ({form.Subquery})";
    }

    // A pattern operator (Contains, StartsWith, EndsWith): the operator says where anything may sit around
    // the value, the dialect says which of its operators matches ignoring case and how that reads the text,
    // and the pattern -- the escaped text with the wildcards -- is the parameter's value. The statement
    // carries an ESCAPE clause every time, so the text is read the same way on every engine.
    private static string BuildPatternCondition(string columnName, FilterCondition condition, SqlBuildContext context)
    {
        var op = condition.Operator;
        var parameterName = $"{context.ParameterPrefix}p{context.ParameterCounter}";
        context.ParameterCounter++;

        // A caller's number or boolean is matched as its text; a list is not a pattern and is refused
        // rather than joined into one.
        object? pattern = condition.Value switch
        {
            null => null,
            string text => LikePattern.Build(text, context.Dialect, op.MatchesAnythingBefore, op.MatchesAnythingAfter),
            IEnumerable => throw new ArgumentException(
                $"Operator '{op.Name}' on '{condition.PropertyName}' takes one value, not a list.",
                nameof(condition)),
            var other => LikePattern.Build(
                Convert.ToString(other, CultureInfo.InvariantCulture)!, // never null: `other` is not
                context.Dialect,
                op.MatchesAnythingBefore,
                op.MatchesAnythingAfter),
        };

        context.AddParam(parameterName.Substring(1), pattern);
        return $"{columnName} {context.Dialect.PatternMatchOperator} {parameterName} {LikePattern.EscapeClause(context.Dialect)}";
    }

    internal static string BuildGroup(FilterGroup group, SqlBuildContext context)
    {
        var clauses = new List<string>();
        foreach (var childNode in group.Nodes)
        {
            var clause = BuildNode(childNode, context);
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
}
