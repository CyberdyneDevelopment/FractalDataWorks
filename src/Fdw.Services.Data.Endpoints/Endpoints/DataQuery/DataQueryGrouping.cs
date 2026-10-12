using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Schema;
using Fdw.Web.RestEndpoints.Query;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Turns the grouped half of a <see cref="DataQueryRequest"/> into the query command the data gateway
/// dispatches: group-by fields, aggregates, the filter before the grouping, an order over the groups and a page
/// of them (FDW-1023).
/// </summary>
/// <remarks>
/// What may be grouped and aggregated is decided by each field's datum type, not by a list of names here: a
/// field is grouped by when its datum type <c>IsGroupable</c>, and aggregated when the type allows the function
/// (<c>AllowsAggregate</c>: Count of anything, Sum and Avg of what <c>IsAggregatable</c>, Min and Max of what
/// <c>IsOrderable</c>). A field the data set computes after the rows are read
/// has no column to group or aggregate in the store, so it is refused. Every refusal is
/// <c>DataQueryInvalid</c> naming the data set and the first thing that did not resolve.
/// </remarks>
internal sealed class DataQueryGrouping
{
    private static readonly Dictionary<string, string> Functions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Sum"] = "SUM",
        ["Avg"] = "AVG",
        ["Min"] = "MIN",
        ["Max"] = "MAX",
        ["Count"] = "COUNT",
    };

    private readonly IReadOnlyList<DataSetFieldConfiguration> _fields;
    private readonly QueryTranslator _filters;
    private readonly IQueryRefusals _refusals;

    /// <summary>Initializes a new instance of the <see cref="DataQueryGrouping"/> class.</summary>
    /// <param name="fields">The data set's current fields.</param>
    /// <param name="filters">The translator that checks a filter against those fields.</param>
    /// <param name="refusals">Builds the refusal a request that does not resolve is answered with.</param>
    public DataQueryGrouping(IReadOnlyList<DataSetFieldConfiguration> fields, QueryTranslator filters, IQueryRefusals refusals)
    {
        _fields = fields;
        _filters = filters;
        _refusals = refusals;
    }

    /// <summary>Gets whether the request asks for a grouped query at all.</summary>
    /// <param name="request">The request body.</param>
    /// <returns>True when the request carries <c>GroupBy</c> or <c>Aggregates</c>.</returns>
    public static bool IsRequested(DataQueryRequest request) => request.GroupBy is not null || request.Aggregates is not null;

    /// <summary>Builds the grouped query, reading one group past the page so a following page can be reported.</summary>
    /// <param name="request">The request body.</param>
    /// <param name="take">The page size, already resolved against the pagination settings.</param>
    /// <returns>The query and the columns its rows carry, or the first refusal.</returns>
    public IGenericResult<GroupedQuery> Translate(DataQueryRequest request, int take)
    {
        if (request.Skip < 0) return _refusals.Invalid<GroupedQuery>("skip must not be negative");

        var groups = Groups(request.GroupBy);
        if (groups.IsFailure) return groups.ToNewResult<GroupedQuery>();

        var measures = Measures(request.Aggregates, groups.Value!);
        if (measures.IsFailure) return measures.ToNewResult<GroupedQuery>();

        if (groups.Value!.Count == 0 && measures.Value!.Count == 0)
            return _refusals.Invalid<GroupedQuery>("a grouped query names at least one group field or one aggregate");

        var filter = _filters.Filter(request.Filter);
        if (filter.IsFailure) return filter.ToNewResult<GroupedQuery>();

        var ordering = Ordering(request.Sort, groups.Value, measures.Value!);
        if (ordering.IsFailure) return ordering.ToNewResult<GroupedQuery>();

        return GenericResult<GroupedQuery>.Success(Build(request, take, groups.Value, measures.Value!, filter.Value, ordering.Value));
    }

    private static GroupedQuery Build(
        DataQueryRequest request,
        int take,
        IReadOnlyList<DataSetFieldConfiguration> groups,
        IReadOnlyList<GroupMeasure> measures,
        IFilterExpression? filter,
        IOrderingExpression? ordering)
        => new(
            new QueryCommand<Dictionary<string, object?>>
            {
                Filter = filter,
                Ordering = ordering,
                Paging = new PagingExpression { Skip = request.Skip, Take = take + 1 },
                Aggregation = new AggregationExpression
                {
                    GroupByFields = [.. groups.Select(g => g.Name)],
                    Aggregations = measures.ToDictionary(m => m.Alias, m => m.Spec, StringComparer.Ordinal),
                    IncludeGroupCount = groups.Count > 0,
                },
            },
            [
                .. groups.Select(g => new DataQueryColumnDto { Name = g.Name, DataType = g.TypeName, Role = g.Role }),
                .. measures.Select(m => m.Column),
            ]);

    private IGenericResult<IReadOnlyList<DataSetFieldConfiguration>> Groups(IReadOnlyList<string>? groupBy)
    {
        var groups = new List<DataSetFieldConfiguration>();
        if (groupBy is null) return GenericResult<IReadOnlyList<DataSetFieldConfiguration>>.Success(groups);

        foreach (var name in groupBy)
        {
            var field = Field(name, "group field");
            if (field.IsFailure) return field.ToNewResult<IReadOnlyList<DataSetFieldConfiguration>>();

            var refusal = Refusal(field.Value!, DatumTypes.ById(field.Value!.DatumTypeId).IsGroupable, "grouped by");
            if (refusal is not null) return _refusals.Invalid<IReadOnlyList<DataSetFieldConfiguration>>(refusal);

            if (groups.Contains(field.Value))
                return _refusals.Invalid<IReadOnlyList<DataSetFieldConfiguration>>($"group field '{field.Value.Name}' is named twice");
            groups.Add(field.Value);
        }

        return GenericResult<IReadOnlyList<DataSetFieldConfiguration>>.Success(groups);
    }

    private IGenericResult<IReadOnlyList<GroupMeasure>> Measures(
        IReadOnlyList<DataQueryAggregate?>? aggregates, IReadOnlyList<DataSetFieldConfiguration> groups)
    {
        var measures = new List<GroupMeasure>();
        var taken = new HashSet<string>(groups.Select(g => g.Name), StringComparer.OrdinalIgnoreCase) { AggregationExpression.GroupCountColumn };
        if (aggregates is null) return GenericResult<IReadOnlyList<GroupMeasure>>.Success(measures);

        foreach (var aggregate in aggregates)
        {
            var measure = Measure(aggregate, taken);
            if (measure.IsFailure) return measure.ToNewResult<IReadOnlyList<GroupMeasure>>();
            measures.Add(measure.Value!);
        }

        return GenericResult<IReadOnlyList<GroupMeasure>>.Success(measures);
    }

    private IGenericResult<GroupMeasure> Measure(DataQueryAggregate? aggregate, HashSet<string> taken)
    {
        if (aggregate is null) return _refusals.Invalid<GroupMeasure>("an aggregate entry must not be null");
        if (!IsPlainName(aggregate.As) || !taken.Add(aggregate.As))
            return _refusals.Invalid<GroupMeasure>(
                $"aggregate name '{aggregate.As}' must be letters, digits and underscores, not start with a digit, and differ from every group field and other aggregate");
        if (aggregate.Function is null || !Functions.TryGetValue(aggregate.Function, out var function))
            return _refusals.Invalid<GroupMeasure>($"aggregate function '{aggregate.Function}' is not Sum, Avg, Min, Max or Count");

        if (string.IsNullOrEmpty(aggregate.Field))
            return string.Equals(function, "COUNT", StringComparison.Ordinal)
                ? GenericResult<GroupMeasure>.Success(new GroupMeasure(aggregate.As, "COUNT(*)", new DataQueryColumnDto { Name = aggregate.As, DataType = DataTypes.Int64.Name }))
                : _refusals.Invalid<GroupMeasure>($"aggregate '{aggregate.As}' needs a field: only Count may name none");

        return OverField(aggregate, function);
    }

    private IGenericResult<GroupMeasure> OverField(DataQueryAggregate aggregate, string function)
    {
        var field = Field(aggregate.Field!, "aggregate field");
        if (field.IsFailure) return field.ToNewResult<GroupMeasure>();

        // Why the datum type decides: the chart check asks the same question, so a chart the check accepts is a
        // grouped read this route accepts.
        var refusal = Refusal(field.Value!, DatumTypes.ById(field.Value!.DatumTypeId).AllowsAggregate(aggregate.Function!), $"given {aggregate.Function}");
        if (refusal is not null) return _refusals.Invalid<GroupMeasure>(refusal);

        var type = function switch
        {
            "COUNT" => DataTypes.Int64.Name,
            "MIN" or "MAX" => field.Value.TypeName,
            _ => DataTypes.Decimal.Name,
        };
        return GenericResult<GroupMeasure>.Success(new GroupMeasure(
            aggregate.As, $"{function}({field.Value.Name})", new DataQueryColumnDto { Name = aggregate.As, DataType = type }));
    }

    private IGenericResult<DataSetFieldConfiguration> Field(string? name, string what)
    {
        var field = name is null ? null : _fields.FirstOrDefault(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));
        if (field is null) return _refusals.Invalid<DataSetFieldConfiguration>($"{what} '{name}' is not a field of this data set");
        if (field.IsCalculated)
            return _refusals.Invalid<DataSetFieldConfiguration>($"'{field.Name}' is calculated after the rows are read, so it cannot be grouped or aggregated");

        return IsPlainName(field.Name)
            ? GenericResult<DataSetFieldConfiguration>.Success(field)
            : _refusals.Invalid<DataSetFieldConfiguration>($"'{field.Name}' is not a plain name (letters, digits and underscores), so the store cannot group or aggregate it");
    }

    private static string? Refusal(DataSetFieldConfiguration field, bool allowed, string action)
        => allowed ? null : $"field '{field.Name}' is typed {DatumTypes.ById(field.DatumTypeId).Name}, which cannot be {action}";

    private IGenericResult<IOrderingExpression?> Ordering(
        IReadOnlyList<QuerySort?>? sort, IReadOnlyList<DataSetFieldConfiguration> groups, IReadOnlyList<GroupMeasure> measures)
    {
        if (sort is not { Count: > 0 }) return GenericResult<IOrderingExpression?>.Success(null);

        var names = groups.Select(g => g.Name).Concat(measures.Select(m => m.Alias)).ToList();
        var ordered = new List<IOrderedField>();
        foreach (var term in sort)
        {
            var name = term?.Field is null ? null : names.FirstOrDefault(n => string.Equals(n, term.Field, StringComparison.OrdinalIgnoreCase));
            if (name is null)
                return _refusals.Invalid<IOrderingExpression?>($"sort field '{term?.Field}' is neither a group field nor an aggregate of this query");

            var direction = SortDirections.ByName(term!.Direction);
            if (ReferenceEquals(direction, SortDirections.NotFound))
                return _refusals.Invalid<IOrderingExpression?>($"sort direction '{term.Direction}' is not Ascending or Descending");

            ordered.Add(new OrderedField { PropertyName = name, Direction = direction });
        }

        return GenericResult<IOrderingExpression?>.Success(new OrderingExpression { OrderedFields = ordered });
    }

    private static bool IsPlainName(string? name)
        => !string.IsNullOrEmpty(name)
           && !char.IsDigit(name[0])
           && name.All(c => (c is >= 'A' and <= 'Z') || (c is >= 'a' and <= 'z') || char.IsDigit(c) || c == '_');

    private sealed record GroupMeasure(string Alias, string Spec, DataQueryColumnDto Column);
}
