using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Protocols.Tds;
using Fdw.Results;
using Fdw.Tds.Endpoints.Sql.Expressions;
using Fdw.Tds.Endpoints.Sql.Rows;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>
/// Runs a query specification over rows held in memory: WHERE, the select list, aggregates, ORDER BY,
/// DISTINCT and paging, each only where its store has not already applied it.
/// </summary>
/// <remarks>
/// GROUP BY and HAVING are refused for now. Aggregates without GROUP BY answer one row over every row kept,
/// and a column read outside an aggregate in such a query is error 8120, as SQL Server reports it.
/// </remarks>
public static class SqlProjection
{
    /// <summary>Projects a query over a row source.</summary>
    /// <param name="query">The query as parsed.</param>
    /// <param name="source">The rows its FROM produced.</param>
    /// <param name="outer">The scope it runs in; an enclosing query's row, for a subquery.</param>
    /// <param name="steps">Which steps run here rather than in the store.</param>
    /// <returns>The result, or the refusal.</returns>
    public static IGenericResult<SqlSelectOutput> Project(QuerySpecification query, SqlRowSet source, SqlScope outer, ProjectionSteps steps)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(outer);

        if (query.GroupByClause is not null || query.HavingClause is not null)
            return SqlRefusal.Refuse<SqlSelectOutput>(SqlServerErrors.NotSupported("GROUP BY", query.StartLine), outer.Context.Logger);

        var scope = outer.Enclose(source.Columns);
        var kept = Kept(query, source, scope, steps);
        if (kept.IsFailure)
            return kept.ToNewResult<SqlSelectOutput>();

        var assignments = query.SelectElements.OfType<SelectSetVariable>().ToList();
        if (assignments.Count > 0)
            return Assign(query, assignments, kept.Value!, scope);

        return Select(query, source, kept.Value!, scope, steps);
    }

    private static IGenericResult<SqlSelectOutput> Select(QuerySpecification query, SqlRowSet source, IReadOnlyList<SqlScope> kept, SqlScope scope, ProjectionSteps steps)
    {
        var items = SelectListPlan.Build(query.SelectElements, source.Columns, scope);
        if (items.IsFailure)
            return items.ToNewResult<SqlSelectOutput>();

        var aggregates = query.SelectElements.SelectMany(FragmentSearch.Aggregates).ToList();
        var rows = aggregates.Count > 0
            ? Aggregated(query, aggregates, items.Value!, kept, scope)
            : Evaluated(items.Value!, kept);
        if (rows.IsFailure)
            return rows.ToNewResult<SqlSelectOutput>();

        var finished = Finish(query, rows.Value!, items.Value!, scope, steps);
        if (finished.IsFailure)
            return finished.ToNewResult<SqlSelectOutput>();

        return GenericResult<SqlSelectOutput>.Success(
            new SqlSelectOutput(items.Value!.Select(i => i.Column).ToList(), finished.Value!, assignedOnly: false));
    }

    // The rows WHERE keeps, each as the scope its values are read in.
    private static IGenericResult<IReadOnlyList<SqlScope>> Kept(QuerySpecification query, SqlRowSet source, SqlScope scope, ProjectionSteps steps)
    {
        var kept = new List<SqlScope>(source.Rows.Count);
        foreach (var row in source.Rows)
        {
            var rowScope = scope.Nest(row);
            if (steps.Filter && query.WhereClause?.SearchCondition is { } condition)
            {
                var holds = SqlPredicates.Evaluate(condition, rowScope);
                if (holds.IsFailure)
                    return holds.ToNewResult<IReadOnlyList<SqlScope>>();

                if (holds.Value != true)
                    continue;
            }

            kept.Add(rowScope);
        }

        return GenericResult<IReadOnlyList<SqlScope>>.Success(kept);
    }

    private static IGenericResult<List<(SqlScope Source, object?[] Values)>> Evaluated(IReadOnlyList<OutputItem> items, IReadOnlyList<SqlScope> kept)
    {
        var rows = new List<(SqlScope, object?[])>(kept.Count);
        foreach (var rowScope in kept)
        {
            var values = Values(items, rowScope);
            if (values.IsFailure)
                return values.ToNewResult<List<(SqlScope, object?[])>>();

            rows.Add((rowScope, values.Value!));
        }

        return GenericResult<List<(SqlScope, object?[])>>.Success(rows);
    }

    private static IGenericResult<List<(SqlScope Source, object?[] Values)>> Aggregated(
        QuerySpecification query, IReadOnlyList<FunctionCall> aggregates, IReadOnlyList<OutputItem> items, IReadOnlyList<SqlScope> kept, SqlScope scope)
    {
        var finder = new UngroupedColumnFinder();
        foreach (var element in query.SelectElements)
            element.Accept(finder);

        if (finder.Found is { } ungrouped)
        {
            return SqlRefusal.Refuse<List<(SqlScope, object?[])>>(new SqlServerMessage(8120, 1, 16,
                $"Column '{SqlText.Of(ungrouped)}' is invalid in the select list because it is not contained in an aggregate function and there is no GROUP BY clause.",
                ungrouped.StartLine), scope.Context.Logger);
        }

        var computed = new Dictionary<FunctionCall, object?>();
        foreach (var call in aggregates)
        {
            var value = SqlAggregates.Compute(call, kept, scope);
            if (value.IsFailure)
                return value.ToNewResult<List<(SqlScope, object?[])>>();

            computed[call] = value.Value;
        }

        var aggregateScope = scope.WithAggregates(computed);
        var values = Values(items, aggregateScope);
        return values.IsFailure
            ? values.ToNewResult<List<(SqlScope, object?[])>>()
            : GenericResult<List<(SqlScope, object?[])>>.Success([(aggregateScope, values.Value!)]);
    }

    private static IGenericResult<object?[]> Values(IReadOnlyList<OutputItem> items, SqlScope rowScope)
    {
        var values = new object?[items.Count];
        for (var i = 0; i < items.Count; i++)
        {
            var value = items[i].Read(rowScope);
            if (value.IsFailure)
                return value.ToNewResult<object?[]>();

            values[i] = value.Value;
        }

        return GenericResult<object?[]>.Success(values);
    }

    // ORDER BY, DISTINCT, then OFFSET/TOP/ROWCOUNT, in SQL Server's order.
    private static IGenericResult<IReadOnlyList<object?[]>> Finish(
        QuerySpecification query, List<(SqlScope Source, object?[] Values)> rows, IReadOnlyList<OutputItem> items, SqlScope scope, ProjectionSteps steps)
    {
        if (steps.Order && query.OrderByClause is { } order)
        {
            var sorted = RowOrdering.Sort(order, rows, items, scope);
            if (sorted.IsFailure)
                return sorted.ToNewResult<IReadOnlyList<object?[]>>();

            rows = sorted.Value!;
        }

        var values = rows.Select(r => r.Values);
        if (query.UniqueRowFilter == UniqueRowFilter.Distinct)
            values = Distinct(values);

        if (!steps.Page)
            return GenericResult<IReadOnlyList<object?[]>>.Success(values.ToList());

        var paging = RowPaging.Of(query, scope);
        return paging.IsFailure
            ? paging.ToNewResult<IReadOnlyList<object?[]>>()
            : GenericResult<IReadOnlyList<object?[]>>.Success(paging.Value.Apply(values).ToList());
    }

    private static List<object?[]> Distinct(IEnumerable<object?[]> rows)
    {
        var distinct = new List<object?[]>();
        foreach (var row in rows)
        {
            if (!distinct.Any(seen => Same(seen, row)))
                distinct.Add(row);
        }

        return distinct;
    }

    // DISTINCT treats two NULLs as the same value, unlike =.
    private static bool Same(object?[] a, object?[] b)
    {
        for (var i = 0; i < a.Length; i++)
        {
            var bothNull = a[i] is null && b[i] is null;
            var equal = a[i] is { } x && b[i] is { } y && SqlValues.TryCompare(x, y, out var order) && order == 0;
            if (!bothNull && !equal)
                return false;
        }

        return true;
    }

    // SELECT @v = expr: every row assigns in turn, so the last row's value is kept; no result set.
    private static IGenericResult<SqlSelectOutput> Assign(QuerySpecification query, List<SelectSetVariable> assignments, IReadOnlyList<SqlScope> kept, SqlScope scope)
    {
        if (assignments.Count != query.SelectElements.Count)
        {
            return SqlRefusal.Refuse<SqlSelectOutput>(new SqlServerMessage(141, 1, 15,
                "A SELECT statement that assigns a value to a variable must not be combined with data-retrieval operations.", query.StartLine), scope.Context.Logger);
        }

        if (assignments.FirstOrDefault(a => a.AssignmentKind != AssignmentKind.Equals) is { } compound)
            return SqlRefusal.Refuse<SqlSelectOutput>(SqlServerErrors.NotSupported(SqlText.Of(compound), compound.StartLine), scope.Context.Logger);

        foreach (var rowScope in kept)
        {
            foreach (var assignment in assignments)
            {
                var assigned = SqlAssignment.Assign(assignment.Variable, assignment.Expression, rowScope);
                if (assigned.IsFailure)
                    return assigned.ToNewResult<SqlSelectOutput>();
            }
        }

        return GenericResult<SqlSelectOutput>.Success(new SqlSelectOutput([], [], assignedOnly: true));
    }
}
