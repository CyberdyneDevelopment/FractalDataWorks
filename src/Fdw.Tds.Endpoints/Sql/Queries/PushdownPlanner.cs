using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Query;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>
/// Plans what a query of a data set's table sends to the store: its filter always, its ordering and paging
/// when the store applies them.
/// </summary>
/// <remarks>
/// <para>
/// The filter, ordering and values pass through the same translator the HTTP query route uses, so a value is
/// checked against its column's declared type the same way whichever door a caller came in by.
/// </para>
/// <para>
/// ORDER BY reaches only a store that orders rows itself (<see cref="SqlTable.PagesInStore"/>); on any other
/// it is refused, as the HTTP route refuses paging there. TOP without ORDER BY is applied to the rows returned
/// when the store cannot page. Aggregates and DISTINCT run over the rows returned, so their query sends the
/// filter alone.
/// </para>
/// </remarks>
internal static class PushdownPlanner
{
    /// <summary>Plans a query of one table.</summary>
    /// <param name="query">The query as parsed.</param>
    /// <param name="table">The table.</param>
    /// <param name="qualifier">The alias or name the query qualifies the table's columns by.</param>
    /// <param name="scope">The scope its values are evaluated in.</param>
    /// <returns>The plan, or the refusal.</returns>
    public static IGenericResult<PushdownPlan> Plan(QuerySpecification query, SqlTable table, string qualifier, SqlScope scope)
    {
        var filter = Filter(query, table, qualifier, scope);
        if (filter.IsFailure)
            return filter.ToNewResult<PushdownPlan>();

        var sorts = Ordering(query, table, scope);
        if (sorts.IsFailure)
            return sorts.ToNewResult<PushdownPlan>();

        var paging = Paging(query, table, scope);
        if (paging.IsFailure)
            return paging.ToNewResult<PushdownPlan>();

        var command = Command(table, filter.Value, sorts.Value!, paging.Value, scope, query.StartLine);
        var steps = new ProjectionSteps(Filter: false, Order: sorts.Value!.Count == 0, Page: paging.Value is null);
        return command.IsFailure
            ? command.ToNewResult<PushdownPlan>()
            : GenericResult<PushdownPlan>.Success(new PushdownPlan(command.Value!, steps));
    }

    // An aggregated query answers one row, so its ordering runs in memory; any other ORDER BY goes to a store
    // that orders rows itself, and is refused by one that does not.
    private static IGenericResult<IReadOnlyList<QuerySort>> Ordering(QuerySpecification query, SqlTable table, SqlScope scope)
    {
        if (query.OrderByClause is null || IsAggregated(query))
            return GenericResult<IReadOnlyList<QuerySort>>.Success([]);

        if (!table.PagesInStore)
            return SqlRefusal.Refuse<IReadOnlyList<QuerySort>>(SqlServerErrors.NotSupported($"ORDER BY on {table}, whose store does not order rows", query.StartLine), scope.Context.Logger);

        return Sorts(query, table, scope);
    }

    // The paging the store applies, or null when it is applied to the rows returned: the store cannot page,
    // or aggregates or DISTINCT must see every row first.
    private static IGenericResult<RowPaging?> Paging(QuerySpecification query, SqlTable table, SqlScope scope)
    {
        if (!table.PagesInStore || IsAggregated(query) || query.UniqueRowFilter == UniqueRowFilter.Distinct)
            return GenericResult<RowPaging?>.Success(null);

        var paging = RowPaging.Of(query, scope);
        if (paging.IsFailure)
            return paging.ToNewResult<RowPaging?>();

        return paging.Value.Skip > 0 || paging.Value.Take is not null
            ? GenericResult<RowPaging?>.Success(paging.Value)
            : GenericResult<RowPaging?>.Success(null);
    }

    private static bool IsAggregated(QuerySpecification query) => query.SelectElements.SelectMany(FragmentSearch.Aggregates).Any();

    private static IGenericResult<QueryFilterNode?> Filter(QuerySpecification query, SqlTable table, string qualifier, SqlScope scope)
    {
        if (query.WhereClause?.SearchCondition is not { } condition)
            return GenericResult<QueryFilterNode?>.Success(null);

        var translated = PushdownFilter.Translate(condition, table, qualifier, scope);
        return translated.IsFailure ? translated.ToNewResult<QueryFilterNode?>() : GenericResult<QueryFilterNode?>.Success(translated.Value);
    }

    private static IGenericResult<IReadOnlyList<QuerySort>> Sorts(QuerySpecification query, SqlTable table, SqlScope scope)
    {
        var sorts = new List<QuerySort>();
        foreach (var element in query.OrderByClause.OrderByElements)
        {
            var column = SortColumn(element.Expression, query, table);
            if (column is null)
            {
                return SqlRefusal.Refuse<IReadOnlyList<QuerySort>>(
                    SqlServerErrors.NotSupported($"ORDER BY {SqlText.Of(element.Expression)} (only a column of the table can be ordered by its store)", element.StartLine),
                    scope.Context.Logger);
            }

            sorts.Add(new QuerySort { Field = column, Direction = element.SortOrder == SortOrder.Descending ? "Descending" : "Ascending" });
        }

        return GenericResult<IReadOnlyList<QuerySort>>.Success(sorts);
    }

    // A sort key the store can apply: a column of the table, named, by alias or by its position in the select list.
    private static string? SortColumn(ScalarExpression expression, QuerySpecification query, SqlTable table)
    {
        var reference = expression switch
        {
            IntegerLiteral position => Positional(position, query, table),
            ColumnReferenceExpression { MultiPartIdentifier: { } parts } => Aliased(parts.Identifiers.Last().Value, query) is { } aliased ? aliased : parts.Identifiers.Last().Value,
            _ => null,
        };

        return reference is null ? null : table.Columns.FirstOrDefault(c => string.Equals(c.Name, reference, StringComparison.OrdinalIgnoreCase))?.Name;
    }

    private static string? Positional(IntegerLiteral position, QuerySpecification query, SqlTable table)
    {
        if (!int.TryParse(position.Value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var ordinal) || ordinal < 1)
            return null;

        if (query.SelectElements is [SelectStarExpression])
            return ordinal <= table.Columns.Count ? table.Columns[ordinal - 1].Name : null;

        return ordinal <= query.SelectElements.Count && query.SelectElements[ordinal - 1] is SelectScalarExpression { Expression: ColumnReferenceExpression { MultiPartIdentifier: { } parts } }
            ? parts.Identifiers.Last().Value
            : null;
    }

    private static string? Aliased(string name, QuerySpecification query)
        => query.SelectElements
            .OfType<SelectScalarExpression>()
            .Where(e => e.ColumnName is { } alias && string.Equals(alias.Value, name, StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Expression is ColumnReferenceExpression { MultiPartIdentifier: { } parts } ? parts.Identifiers.Last().Value : null)
            .FirstOrDefault();

    private static IGenericResult<QueryCommand<Dictionary<string, object?>>> Command(
        SqlTable table, QueryFilterNode? filter, IReadOnlyList<QuerySort> sorts, RowPaging? paging, SqlScope scope, int line)
    {
        var fields = table.Columns.Select(c => new QueryableField(c.Name, c.Name, c.ClrType, !c.IsQueryable, c.IsFilterable));
        var translator = QueryTranslator.Create(fields, $"table {table}", new SqlQueryRefusals(scope, line));
        if (translator.IsFailure)
            return translator.ToNewResult<QueryCommand<Dictionary<string, object?>>>();

        // Why here: the whole WHERE of a data set's table is sent to the store, so this is the one filter the caller asked for,
        // and the same refusal the HTTP routes give a filter on a field the data set has not made filterable (FDW-1075).
        var filterable = translator.Value!.RequireFilterable(filter);
        if (filterable.IsFailure)
            return filterable.ToNewResult<QueryCommand<Dictionary<string, object?>>>();

        var domain = translator.Value!.ToDomainQuery(new ResourceQueryRequest { Filter = filter, Sort = sorts.ToList() }, 0);
        if (domain.IsFailure)
            return domain.ToNewResult<QueryCommand<Dictionary<string, object?>>>();

        return GenericResult<QueryCommand<Dictionary<string, object?>>>.Success(new QueryCommand<Dictionary<string, object?>>
        {
            Filter = domain.Value!.Filter,
            Ordering = domain.Value.Ordering,
            Paging = paging is { } page ? new PagingExpression { Skip = (int)Math.Min(page.Skip, int.MaxValue), Take = Take(page) } : null,
        });
    }

    private static int? Take(RowPaging page) => page.Take is { } take ? (int)Math.Min(take, int.MaxValue) : null;
}
