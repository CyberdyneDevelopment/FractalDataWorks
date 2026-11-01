using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Web.RestEndpoints.Query;

/// <summary>
/// Turns a <see cref="QueryRequest"/> body into the filter, ordering and page a store is asked for,
/// checked against the fields the resource declares.
/// </summary>
/// <remarks>
/// Every name in the body — filter field, sort field, operator, direction, group logic — is resolved
/// against the declared fields or a TypeCollection, and anything that does not resolve refuses the
/// whole query. Nothing is skipped: a dropped filter widens the result, and a caller cannot tell that
/// it happened. This is the one translation a data set's rows and a configuration resource's list
/// share; the resource supplies its fields and how a refusal reads, and nothing else differs.
/// </remarks>
public sealed class QueryTranslator
{
    // SQL Server allows 2,100 parameters per statement and every In value is one. A thousand leaves
    // room for the rest of the query and turns an oversize list into a 400 rather than a store error.
    private const int MaxInValues = 1000;

    // Between and NotIn are the API's words for a range and an exclusive list. Neither is a registered operator: each is
    // read here into operators every connection already renders (>= and <=; <> per value), so no store, protocol or
    // record matcher has to learn them.
    private const string BetweenName = "Between";
    private const string NotInName = "NotIn";

    // The body member a filter tree is read from, as a refusal names it.
    private const string FilterPath = "filter";

    private static readonly HashSet<Type> NumberTypes = [typeof(byte), typeof(short), typeof(int), typeof(long), typeof(decimal), typeof(double), typeof(float)];
    private static readonly HashSet<Type> DateTypes = [typeof(DateTime), typeof(DateTimeOffset)];
    private static readonly HashSet<Type> ListTypes = [typeof(string), typeof(byte), typeof(short), typeof(int), typeof(long)];

    private readonly Dictionary<string, QueryableField> _fields;
    private readonly string _subject;
    private readonly IQueryRefusals _refusals;
    private readonly bool _allowCalculated;
    private readonly HashSet<string> _calculatedColumns;

    private QueryTranslator(Dictionary<string, QueryableField> fields, string subject, IQueryRefusals refusals, bool allowCalculated)
    {
        _fields = fields;
        _subject = subject;
        _refusals = refusals;
        _allowCalculated = allowCalculated;
        _calculatedColumns = fields.Values.Where(f => f.IsCalculated).Select(f => f.Column).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Creates a translator over the fields a resource declares.</summary>
    /// <param name="fields">The fields a query may name.</param>
    /// <param name="subject">What the fields belong to, as a refusal names it: "this data set", "users".</param>
    /// <param name="refusals">Builds the refusals the caller is answered with.</param>
    /// <param name="allowCalculated">
    /// Whether a filter or sort may name a calculated field (FDW-1045). False refuses it, as the store cannot run it; true is
    /// for a caller that plans the query with <see cref="Plan"/> and applies that part itself after the read.
    /// </param>
    /// <returns>The translator, or a refusal when two fields' names differ only by case.</returns>
    /// <remarks>
    /// Field names in a request are matched case-insensitively, so two fields that differ only by case
    /// could never be told apart. That is refused rather than resolved to whichever came first.
    /// </remarks>
    public static IGenericResult<QueryTranslator> Create(
        IEnumerable<QueryableField> fields, string subject, IQueryRefusals refusals, bool allowCalculated = false)
    {
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(refusals);

        var byName = new Dictionary<string, QueryableField>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in fields)
        {
            if (!byName.TryAdd(field.Name, field))
                return refusals.Invalid<QueryTranslator>($"{subject} has more than one field named '{field.Name}' ignoring case");
        }

        return GenericResult<QueryTranslator>.Success(new QueryTranslator(byName, subject, refusals, allowCalculated));
    }

    /// <summary>Builds the query command a data gateway dispatches, reading one row past the page so a following page can be reported.</summary>
    /// <param name="request">The request body.</param>
    /// <param name="take">The page size, already resolved against the pagination settings.</param>
    /// <returns>The command, or a refusal naming the first thing that did not resolve.</returns>
    public IGenericResult<QueryCommand<Dictionary<string, object?>>> Translate(QueryRequest request, int take)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = Resolve(request);
        if (query.IsFailure) return query.ToNewResult<QueryCommand<Dictionary<string, object?>>>();

        return GenericResult<QueryCommand<Dictionary<string, object?>>>.Success(new QueryCommand<Dictionary<string, object?>>
        {
            Filter = query.Value.Filter,
            Ordering = query.Value.Ordering,
            Paging = new PagingExpression { Skip = request.Skip, Take = take + 1 },
        });
    }

    /// <summary>
    /// Plans a query that may name calculated fields: what the store runs, and what is applied to the rows after they are read
    /// (FDW-1045).
    /// </summary>
    /// <param name="request">The request body.</param>
    /// <param name="take">The page size, already resolved against the pagination settings.</param>
    /// <param name="rowCap">The most rows the store may be asked for when part of the query runs in memory.</param>
    /// <returns>The plan, or a refusal naming the first thing that did not resolve.</returns>
    /// <remarks>
    /// A filter's top-level And is split: the conjuncts that name no calculated field go to the store, and the rest run in
    /// memory. A single condition, or an Or, goes wholly one way or the other. A sort that names a calculated field runs in
    /// memory over the rows read; any other sort stays in the store. When part runs in memory the page is cut there too, and
    /// the store is asked for one row past <paramref name="rowCap"/> so a read over the cap can be told from one at it. A
    /// query that names no calculated field is the plain command with its page in the store.
    /// </remarks>
    public IGenericResult<QueryPlan> Plan(QueryRequest request, int take, int rowCap)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = Resolve(request);
        if (query.IsFailure) return query.ToNewResult<QueryPlan>();

        var (pushed, remaining) = SplitFilter(query.Value.Filter?.Root);
        var sortsInMemory = OrdersByCalculated(query.Value.Ordering);
        var inMemory = remaining is not null || sortsInMemory;

        return GenericResult<QueryPlan>.Success(new QueryPlan
        {
            Command = new QueryCommand<Dictionary<string, object?>>
            {
                Filter = pushed is null ? null : new FilterExpression { Root = pushed },
                Ordering = sortsInMemory ? null : query.Value.Ordering,
                Paging = inMemory
                    ? new PagingExpression { Skip = 0, Take = rowCap + 1 }
                    : new PagingExpression { Skip = request.Skip, Take = take + 1 },
            },
            InMemoryFilter = remaining,
            InMemoryOrdering = sortsInMemory ? query.Value.Ordering : null,
            Skip = request.Skip,
            Take = take,
            RowCap = rowCap,
        });
    }

    private bool OrdersByCalculated(IOrderingExpression? ordering)
        => ordering is not null && ordering.OrderedFields.Any(field => _calculatedColumns.Contains(field.PropertyName));

    private (IFilterNode? Pushed, IFilterNode? Remaining) SplitFilter(IFilterNode? root)
    {
        if (root is null) return (null, null);
        if (root is not FilterGroup group || !ReferenceEquals(group.Operator, LogicalOperator.And))
            return NamesCalculated(root) ? (null, root) : (root, null);

        return (AllOf(group.Nodes.Where(node => !NamesCalculated(node)).ToList()), AllOf(group.Nodes.Where(NamesCalculated).ToList()));
    }

    private static IFilterNode? AllOf(List<IFilterNode> nodes)
        => nodes.Count switch
        {
            0 => null,
            1 => nodes[0],
            _ => new FilterGroup { Operator = LogicalOperator.And, Nodes = nodes },
        };

    private bool NamesCalculated(IFilterNode node)
        => node switch
        {
            FilterCondition condition => _calculatedColumns.Contains(condition.PropertyName),
            FilterGroup group => group.Nodes.Any(NamesCalculated),
            _ => false,
        };

    /// <summary>Builds the query a domain provider answers from its domain table.</summary>
    /// <param name="request">The request body.</param>
    /// <param name="take">The page size, already resolved against the pagination settings.</param>
    /// <returns>The query, or a refusal naming the first thing that did not resolve.</returns>
    public IGenericResult<DomainQuery> ToDomainQuery(ResourceQueryRequest request, int take)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = Resolve(request);
        return query.IsFailure
            ? query.ToNewResult<DomainQuery>()
            : GenericResult<DomainQuery>.Success(
                new DomainQuery(query.Value.Filter, query.Value.Ordering, request.Skip, take, request.IncludeTotal));
    }

    /// <summary>Refuses a filter tree that names a field a caller may not filter on.</summary>
    /// <remarks>
    /// Run on the caller's own filter before a route merges its own into it, so a view's stored filter may use a field
    /// the caller may not. A field this translator does not know is left for <see cref="Filter"/> to refuse, which says so.
    /// </remarks>
    /// <param name="node">The caller's filter tree, or null for no filter.</param>
    /// <returns>Success, or a refusal naming the first field the caller may not filter on.</returns>
    public IGenericResult RequireFilterable(QueryFilterNode? node)
    {
        if (node is null) return GenericResult.Success();

        if (node.Field is not null && _fields.TryGetValue(node.Field, out var field) && FieldRefusal(node, field) is { } refused)
            return refused;

        if (node.Nodes is null) return GenericResult.Success();

        foreach (var child in node.Nodes)
        {
            var checkedChild = RequireFilterable(child);
            if (checkedChild.IsFailure) return checkedChild;
        }

        return GenericResult.Success();
    }

    // The first reason the caller may not filter on this field with this condition, or null when it may: the field is not
    // filterable, or the condition's operator is not one the field takes (FDW-1102).
    private IGenericResult? FieldRefusal(QueryFilterNode node, QueryableField field)
    {
        if (!field.IsFilterable)
            return _refusals.Invalid<bool>($"'{field.Name}' is not a field of {_subject} that can be filtered on");

        if (field.FilterOperators is not { } allowed) return null;

        // A date range names no operator: it is the range form, which a field takes as Between.
        var used = node.StartDate is not null || node.EndDate is not null ? BetweenName : node.Operator;
        if (used is null || allowed.Contains(used, StringComparer.OrdinalIgnoreCase)) return null;

        return _refusals.Invalid<bool>(allowed.Count == 0
            ? field.NoOperatorsReason ?? $"'{field.Name}' takes no filter operator"
            : $"operator '{used}' cannot be used on '{field.Name}'; it takes {string.Join(", ", allowed)}");
    }

    /// <summary>
    /// Replaces the calculation references in a filter tree with the values the calculations yield, checked against the type of the
    /// field each is compared with. Run it on a filter before translating it; the translator refuses a reference that is still there.
    /// </summary>
    /// <param name="node">The filter tree, or null for no filter. It is not changed.</param>
    /// <param name="calculations">Evaluates the calculations; null for a host that has none, which refuses any reference.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The tree with a literal where each reference was, or a refusal that names the field and the calculation.</returns>
    public Task<IGenericResult<QueryFilterNode?>> ResolveCalculations(
        QueryFilterNode? node, IFilterCalculations? calculations, CancellationToken cancellationToken = default)
        => new FilterCalculationResolver(_fields, _refusals, calculations).Resolve(node, cancellationToken);

    /// <summary>Builds the filter a request's tree asks for, checked against the fields this translator knows.</summary>
    /// <param name="node">The filter tree, or null for no filter.</param>
    /// <returns>The filter (null when there is none), or a refusal naming the first thing that did not resolve.</returns>
    public IGenericResult<IFilterExpression?> Filter(QueryFilterNode? node)
    {
        if (node is null) return GenericResult<IFilterExpression?>.Success(null);

        var root = Node(node, FilterPath);
        return root.IsFailure
            ? root.ToNewResult<IFilterExpression?>()
            : GenericResult<IFilterExpression?>.Success(new FilterExpression { Root = root.Value! });
    }

    private IGenericResult<(IFilterExpression? Filter, IOrderingExpression? Ordering)> Resolve(QueryRequest request)
    {
        if (request.Skip < 0)
            return _refusals.Invalid<(IFilterExpression?, IOrderingExpression?)>("skip must not be negative");

        var filter = request.Filter is null ? null : Node(request.Filter, FilterPath);
        if (filter is { IsFailure: true }) return filter.ToNewResult<(IFilterExpression?, IOrderingExpression?)>();

        var ordering = Ordering(request.Sort);
        if (ordering.IsFailure) return ordering.ToNewResult<(IFilterExpression?, IOrderingExpression?)>();

        return GenericResult<(IFilterExpression?, IOrderingExpression?)>.Success((
            filter is null ? null : new FilterExpression { Root = filter.Value! },
            ordering.Value));
    }

    // Every refusal of the tree's shape names the node by its path in the body -- filter, filter.nodes[0], filter.logic --
    // so a caller sent a null node or a group with no logic is told which member to fix (FDW-1125).
    private IGenericResult<IFilterNode> Node(QueryFilterNode? node, string path)
    {
        if (node is null)
            return _refusals.Invalid<IFilterNode>(
                $"{path} is null; each filter node must be either a group (logic + nodes) or a condition (field + operator [+ value])");

        var isGroup = node.Logic is not null || node.Nodes is not null;
        var isCondition = node.Field is not null || node.Operator is not null || node.Value is not null
            || node.StartDate is not null || node.EndDate is not null;
        if (isGroup == isCondition)
            return _refusals.Invalid<IFilterNode>(
                $"{path} must be either a group (logic + nodes) or a condition (field + operator [+ value], or a date field + startDate and/or endDate)");

        return isGroup ? Group(node, path) : Condition(node);
    }

    private IGenericResult<IFilterNode> Group(QueryFilterNode node, string path)
    {
        if (node.Logic is null)
            return _refusals.Invalid<IFilterNode>($"{path}.logic is missing; a filter group's logic is And or Or");

        var logic = new[] { LogicalOperator.And, LogicalOperator.Or }
            .FirstOrDefault(o => string.Equals(o.Name, node.Logic, StringComparison.OrdinalIgnoreCase));
        if (logic is null)
            return _refusals.Invalid<IFilterNode>($"{path}.logic '{node.Logic}' is not And or Or");
        if (node.Nodes is not { Count: > 0 })
            return _refusals.Invalid<IFilterNode>($"{path}.nodes must contain at least one node");

        var children = new List<IFilterNode>();
        for (var index = 0; index < node.Nodes.Count; index++)
        {
            var translated = Node(node.Nodes[index], string.Create(CultureInfo.InvariantCulture, $"{path}.nodes[{index}]"));
            if (translated.IsFailure) return translated;
            children.Add(translated.Value!);
        }

        return GenericResult<IFilterNode>.Success(new FilterGroup { Operator = logic, Nodes = children });
    }

    private IGenericResult<IFilterNode> Condition(QueryFilterNode node)
    {
        if (node.Field is null || !_fields.TryGetValue(node.Field, out var field))
            return _refusals.Invalid<IFilterNode>($"filter field '{node.Field}' is not a field of {_subject}");
        if (field.IsCalculated && !_allowCalculated)
            return _refusals.Invalid<IFilterNode>($"'{field.Name}' is calculated after the rows are read, so it cannot be filtered on");
        if (FilterCalculationReference.HoldsReference(node.Value) || FilterCalculationReference.HoldsReference(node.StartDate)
            || FilterCalculationReference.HoldsReference(node.EndDate))
            return _refusals.Invalid<IFilterNode>(
                $"filter condition on '{field.Name}' still holds a calculation: calculations are resolved with ResolveCalculations before a filter is translated");
        if (Composite(node, field) is { } composite)
            return composite;
        if (node.Operator is null)
            return _refusals.Invalid<IFilterNode>($"filter condition on '{field.Name}' names no operator");

        var op = FilterOperators.ByName(node.Operator);
        if (ReferenceEquals(op, FilterOperators.NotFound))
            return _refusals.Invalid<IFilterNode>($"filter operator '{node.Operator}' is not a registered operator");

        var value = Value(op, node.Value, field);
        if (value.IsFailure) return value.ToNewResult<IFilterNode>();

        return GenericResult<IFilterNode>.Success(
            new FilterCondition { PropertyName = field.Column, Operator = op, Value = value.Value });
    }

    private IGenericResult<object?> Value(IFilterOperator op, JsonElement? value, QueryableField field)
    {
        var fieldName = field.Name;
        var absent = value is not { } present || present.ValueKind == JsonValueKind.Null;
        if (!op.RequiresValue)
            return absent
                ? GenericResult<object?>.Success(null)
                : _refusals.Invalid<object?>($"operator '{op.Name}' on '{fieldName}' takes no value");
        if (absent)
            return _refusals.Invalid<object?>($"operator '{op.Name}' on '{fieldName}' requires a value");

        // A pattern operator matches text: its value is the text to find, whatever type the field holds.
        var valueType = op.IsPatternMatch ? null : field.ValueType;

        if (!ReferenceEquals(op, FilterOperators.ByName("In")))
            return Typed(value!.Value, valueType, fieldName);

        var items = ListOf(value, field, op.Name);
        return items.IsFailure ? items.ToNewResult<object?>() : GenericResult<object?>.Success(items.Value);
    }

    // A list is a non-empty JSON array of at most MaxInValues values, each checked against the field's type, so one bad
    // element refuses the whole query. In and NotIn read theirs here.
    private IGenericResult<List<object?>> ListOf(JsonElement? value, QueryableField field, string operatorName)
    {
        if (value is not { } list || list.ValueKind != JsonValueKind.Array || list.GetArrayLength() == 0)
            return _refusals.Invalid<List<object?>>($"operator '{operatorName}' on '{field.Name}' requires a non-empty array");
        if (list.GetArrayLength() > MaxInValues)
            return _refusals.Invalid<List<object?>>($"operator '{operatorName}' on '{field.Name}' takes at most {MaxInValues} values");

        var items = new List<object?>();
        foreach (var element in list.EnumerateArray())
        {
            var item = Typed(element, field.ValueType, field.Name);
            if (item.IsFailure) return item.ToNewResult<List<object?>>();
            items.Add(item.Value);
        }

        return GenericResult<List<object?>>.Success(items);
    }

    // The API's own condition forms, read into existing operators: a date range (startDate and/or endDate), Between and
    // NotIn. Null means the node is an ordinary operator condition.
    private IGenericResult<IFilterNode>? Composite(QueryFilterNode node, QueryableField field)
    {
        if (node.StartDate is not null || node.EndDate is not null) return DateRange(node, field);
        if (Named(node.Operator, BetweenName)) return Between(node, field);
        return Named(node.Operator, NotInName) ? NotIn(node, field) : null;
    }

    private static bool Named(string? actual, string expected)
        => string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);

    // Between is for number fields and takes {"start": .., "stop": ..}, both ends inclusive. A date field takes startDate and
    // endDate on its condition instead (DateRange), so Between on one is refused with that pointer.
    private IGenericResult<IFilterNode> Between(QueryFilterNode node, QueryableField field)
    {
        if (field.ValueType is not { } type || !NumberTypes.Contains(type))
            return _refusals.Invalid<IFilterNode>(
                $"operator 'Between' on '{field.Name}' takes a number field; a date field takes startDate and endDate");

        var bounds = Bounds(node.Value, field.Name);
        if (bounds.IsFailure) return bounds.ToNewResult<IFilterNode>();

        var low = Typed(bounds.Value.Low, type, field.Name);
        if (low.IsFailure) return low.ToNewResult<IFilterNode>();

        var high = Typed(bounds.Value.High, type, field.Name);
        if (high.IsFailure) return high.ToNewResult<IFilterNode>();

        return FilterValueComparer.Compare(low.Value, high.Value) > 0
            ? _refusals.Invalid<IFilterNode>($"'start' on '{field.Name}' is after 'stop'")
            : Bounded(field, low.Value, high.Value, wholeEndDay: false);
    }

    // A date field's range is startDate and/or endDate on its condition, in place of an operator and a value: DateTime and
    // DateTimeOffset fields only. Both ends are inclusive, and an endDate written with no time ("2026-01-31") keeps the whole
    // of that day. Either end may stand alone.
    private IGenericResult<IFilterNode> DateRange(QueryFilterNode node, QueryableField field)
    {
        if (field.ValueType is not { } type || !DateTypes.Contains(type))
            return _refusals.Invalid<IFilterNode>(
                $"startDate and endDate on '{field.Name}' need a date field; a number field takes the Between operator");
        if (node.Operator is not null || node.Value is not null)
            return _refusals.Invalid<IFilterNode>(
                $"filter condition on '{field.Name}' is an operator with a value or a startDate and endDate range, not both");

        var start = DateBound(node.StartDate, field);
        if (start.IsFailure) return start.ToNewResult<IFilterNode>();

        var end = DateBound(node.EndDate, field);
        if (end.IsFailure) return end.ToNewResult<IFilterNode>();

        return FilterValueComparer.Compare(start.Value, end.Value) > 0
            ? _refusals.Invalid<IFilterNode>($"'startDate' on '{field.Name}' is after 'endDate'")
            : Bounded(field, start.Value, end.Value, node.EndDate is { } last && IsDateOnly(last, field.Name));
    }

    // An end that is not named is absent (null); a named one is always a typed, non-null date.
    private IGenericResult<object?> DateBound(JsonElement? bound, QueryableField field)
        => bound is { } given ? Typed(given, field.ValueType, field.Name) : GenericResult<object?>.Success(null);

    private IGenericResult<(JsonElement Low, JsonElement High)> Bounds(JsonElement? value, string fieldName)
    {
        var shape = $"operator 'Between' on '{fieldName}' takes a value of the form {{\"start\": ..., \"stop\": ...}}";
        if (value is not { } given || !IsObject(given))
            return _refusals.Invalid<(JsonElement Low, JsonElement High)>(shape);

        JsonElement? low = null;
        JsonElement? high = null;
        var count = 0;
        foreach (var property in given.EnumerateObject())
        {
            count++;
            if (Named(property.Name, "start")) low = property.Value;
            else if (Named(property.Name, "stop")) high = property.Value;
        }

        return count == 2 && low is { } start && high is { } stop
            ? GenericResult<(JsonElement Low, JsonElement High)>.Success((start, stop))
            : _refusals.Invalid<(JsonElement Low, JsonElement High)>(shape);
    }

    private static bool IsObject(JsonElement element)
    {
#pragma warning disable FDW018 // External System.Text.Json JsonValueKind enum — cannot convert to TypeCollection
        return element.ValueKind == JsonValueKind.Object;
#pragma warning restore FDW018
    }

    private static bool IsDateOnly(JsonElement element, string fieldName)
        => Reading.Of(element, fieldName) is { IsText: true } reading
           && DateOnly.TryParseExact(reading.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

    // The conditions a range makes: the field at or after the low end and at or before the high end. A null end is absent (a
    // date range may name one end) and at least one is always present. A whole end day stops before the next midnight.
    private static IGenericResult<IFilterNode> Bounded(QueryableField field, object? low, object? high, bool wholeEndDay)
    {
        var bounds = new List<IFilterNode>();
        if (low is not null) bounds.Add(Leaf(field, "GreaterThanOrEqual", low));
        if (high is not null)
            bounds.Add(wholeEndDay ? Leaf(field, "LessThan", NextDay(high)) : Leaf(field, "LessThanOrEqual", high));

        return GenericResult<IFilterNode>.Success(
            bounds.Count == 1 ? bounds[0] : new FilterGroup { Operator = LogicalOperator.And, Nodes = bounds });
    }

    private static object NextDay(object? date)
        => date is DateTimeOffset moment ? (object)moment.AddDays(1) : (object)((DateTime)date!).AddDays(1);

    // NotIn is every value excluded: the field differs from each one. A null in the list is refused with the list's other
    // values (ListOf), because a field is never "different from null" in SQL and the list would silently match nothing.
    private IGenericResult<IFilterNode> NotIn(QueryFilterNode node, QueryableField field)
    {
        if (field.ValueType is not { } type || !ListTypes.Contains(type))
            return _refusals.Invalid<IFilterNode>($"operator 'NotIn' on '{field.Name}' takes a string or whole-number field");

        var items = ListOf(node.Value, field, NotInName);
        if (items.IsFailure) return items.ToNewResult<IFilterNode>();

        var excluded = items.Value!.Select(item => (IFilterNode)Leaf(field, "NotEqual", item)).ToList();
        return GenericResult<IFilterNode>.Success(
            excluded.Count == 1 ? excluded[0] : new FilterGroup { Operator = LogicalOperator.And, Nodes = excluded });
    }

    private static FilterCondition Leaf(QueryableField field, string operatorName, object? value)
        => new() { PropertyName = field.Column, Operator = FilterOperators.ByName(operatorName), Value = value };

    // A value is checked against the type the field declares before any command is built. A number or a
    // GUID is accepted as JSON gives it or as text (a form sends text); anything that does not parse is a
    // refusal that names the field, never an error from the store. A date or a time is ISO 8601 text, and one
    // written without an offset is read as UTC -- the same for every caller, whatever the server's clock says.
    private IGenericResult<object?> Typed(JsonElement element, Type? type, string fieldName)
    {
        if (type is null)
            return Scalar(element, fieldName);

        // Nothing but a JSON string, number or boolean is a value here; a type with no parser below (binary,
        // a single character) is passed as the JSON gave it.
        var reading = Reading.Of(element, fieldName);
        if (reading is null || !Parsers.TryGetValue(type, out var parse))
            return Scalar(element, fieldName);

        return parse(this, reading.Value);
    }

    // One parser per declared type, so adding a type is one entry and no method grows with it.
    private static readonly Dictionary<Type, Func<QueryTranslator, Reading, IGenericResult<object?>>> Parsers = new()
    {
        [typeof(string)] = static (_, reading) => AsText(reading),
        [typeof(Guid)] = static (translator, reading) => translator.AsGuid(reading),
        [typeof(bool)] = static (translator, reading) => translator.AsBool(reading),
        [typeof(long)] = static (translator, reading) => translator.Whole(reading.Text, typeof(long), reading.FieldName),
        [typeof(int)] = static (translator, reading) => translator.Whole(reading.Text, typeof(int), reading.FieldName),
        [typeof(short)] = static (translator, reading) => translator.Whole(reading.Text, typeof(short), reading.FieldName),
        [typeof(byte)] = static (translator, reading) => translator.Whole(reading.Text, typeof(byte), reading.FieldName),
        [typeof(decimal)] = static (translator, reading) => translator.AsDecimal(reading),
        [typeof(double)] = static (translator, reading) => translator.AsReal(reading, typeof(double)),
        [typeof(float)] = static (translator, reading) => translator.AsReal(reading, typeof(float)),
        [typeof(DateTimeOffset)] = static (translator, reading) => translator.AsMoment(reading),
        [typeof(DateTime)] = static (translator, reading) => translator.AsDate(reading),
        [typeof(TimeSpan)] = static (translator, reading) => translator.AsSpan(reading),
    };

    private static IGenericResult<object?> AsText(Reading reading)
        => GenericResult<object?>.Success(reading.IsBool ? (reading.Element.GetBoolean() ? "true" : "false") : reading.Text);

    private IGenericResult<object?> AsGuid(Reading reading)
        => reading.IsText && Guid.TryParse(reading.Text, out var guid)
            ? GenericResult<object?>.Success(guid)
            : Wrong(reading.FieldName, "a GUID", reading.Text);

    private IGenericResult<object?> AsBool(Reading reading)
        => reading.IsBool ? GenericResult<object?>.Success(reading.Element.GetBoolean())
            : reading.IsText && bool.TryParse(reading.Text, out var flag) ? GenericResult<object?>.Success(flag)
            : Wrong(reading.FieldName, "true or false", reading.Text);

    private IGenericResult<object?> AsDecimal(Reading reading)
        => !reading.IsBool && decimal.TryParse(reading.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var money)
            ? GenericResult<object?>.Success(money)
            : Wrong(reading.FieldName, "a number", reading.Text);

    private IGenericResult<object?> AsReal(Reading reading, Type type)
        => !reading.IsBool && double.TryParse(reading.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var real)
           && !double.IsNaN(real) && !double.IsInfinity(real)
            ? GenericResult<object?>.Success(type == typeof(float) ? (object)(float)real : real)
            : Wrong(reading.FieldName, "a number", reading.Text);

    private IGenericResult<object?> AsMoment(Reading reading)
        => reading.IsText && DateTimeOffset.TryParse(reading.Text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var moment)
            ? GenericResult<object?>.Success(moment)
            : Wrong(reading.FieldName, "an ISO 8601 date or time", reading.Text);

    private IGenericResult<object?> AsDate(Reading reading)
        => reading.IsText && DateTime.TryParse(reading.Text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var date)
            ? GenericResult<object?>.Success(date)
            : Wrong(reading.FieldName, "an ISO 8601 date or time", reading.Text);

    private IGenericResult<object?> AsSpan(Reading reading)
        => reading.IsText && TimeSpan.TryParse(reading.Text, CultureInfo.InvariantCulture, out var span)
            ? GenericResult<object?>.Success(span)
            : Wrong(reading.FieldName, "a time span", reading.Text);

    // A JSON string, number or boolean read once: its text, and which of the three it was.
    private readonly record struct Reading(JsonElement Element, string Text, bool IsText, bool IsBool, string FieldName)
    {
        public static Reading? Of(JsonElement element, string fieldName)
        {
#pragma warning disable FDW018 // External System.Text.Json JsonValueKind enum — cannot convert to TypeCollection
            var kind = element.ValueKind;
            var isText = kind == JsonValueKind.String;
            var isNumber = kind == JsonValueKind.Number;
            var isBool = kind is JsonValueKind.True or JsonValueKind.False;
#pragma warning restore FDW018
            if (!isText && !isNumber && !isBool)
                return null;

            return new Reading(element, isText ? element.GetString()! : element.GetRawText(), isText, isBool, fieldName);
        }
    }

    private IGenericResult<object?> Whole(string text, Type type, string fieldName)
    {
        if (!long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var whole))
            return Wrong(fieldName, "a whole number", text);

        if (type == typeof(long)) return GenericResult<object?>.Success(whole);
        if (type == typeof(int))
            return whole is >= int.MinValue and <= int.MaxValue
                ? GenericResult<object?>.Success((int)whole)
                : Wrong(fieldName, "a whole number in the range of the field", text);
        if (type == typeof(short))
            return whole is >= short.MinValue and <= short.MaxValue
                ? GenericResult<object?>.Success((short)whole)
                : Wrong(fieldName, "a whole number in the range of the field", text);
        return whole is >= byte.MinValue and <= byte.MaxValue
            ? GenericResult<object?>.Success((byte)whole)
            : Wrong(fieldName, "a whole number in the range of the field", text);
    }

    // The refusal names the field and what it holds. The value itself is not repeated in full: a caller's
    // text can be long, and the field is what a client needs to correct.
    private IGenericResult<object?> Wrong(string fieldName, string expected, string text)
        => _refusals.Invalid<object?>(
            $"value for '{fieldName}' must be {expected}, and '{(text.Length > 40 ? text.Substring(0, 40) + "..." : text)}' is not");

    private IGenericResult<object?> Scalar(JsonElement element, string fieldName)
    {
#pragma warning disable FDW018 // External System.Text.Json JsonValueKind enum — cannot convert to TypeCollection
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                return GenericResult<object?>.Success(element.GetString());
            case JsonValueKind.True:
                return GenericResult<object?>.Success(true);
            case JsonValueKind.False:
                return GenericResult<object?>.Success(false);
            case JsonValueKind.Number when element.TryGetInt64(out var whole):
                return GenericResult<object?>.Success(whole);
            case JsonValueKind.Number when element.TryGetDecimal(out var fractional):
                return GenericResult<object?>.Success(fractional);
            case JsonValueKind.Number:
                return _refusals.Invalid<object?>($"value for '{fieldName}' is a number outside the range a filter can carry");
            default:
                return _refusals.Invalid<object?>($"value for '{fieldName}' must be a string, number or boolean, not {element.ValueKind}");
        }
#pragma warning restore FDW018
    }

    private IGenericResult<IOrderingExpression?> Ordering(IReadOnlyList<QuerySort?>? sort)
    {
        if (sort is not { Count: > 0 }) return GenericResult<IOrderingExpression?>.Success(null);

        var ordered = new List<IOrderedField>();
        foreach (var term in sort)
        {
            if (term is null)
                return _refusals.Invalid<IOrderingExpression?>("a sort entry must not be null");
            // Field and Direction are non-nullable in the DTO, but a JSON null still lands as null.
            if (term.Field is null || !_fields.TryGetValue(term.Field, out var field))
                return _refusals.Invalid<IOrderingExpression?>($"sort field '{term.Field}' is not a field of {_subject}");
            if (field.IsCalculated && !_allowCalculated)
                return _refusals.Invalid<IOrderingExpression?>($"'{field.Name}' is calculated after the rows are read, so it cannot be sorted on");
            if (term.Direction is null)
                return _refusals.Invalid<IOrderingExpression?>($"sort on '{field.Name}' names no direction");

            var direction = SortDirections.ByName(term.Direction);
            if (ReferenceEquals(direction, SortDirections.NotFound))
                return _refusals.Invalid<IOrderingExpression?>($"sort direction '{term.Direction}' is not Ascending or Descending");

            ordered.Add(new OrderedField { PropertyName = field.Column, Direction = direction });
        }

        return GenericResult<IOrderingExpression?>.Success(new OrderingExpression { OrderedFields = ordered });
    }
}
