using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Data.Tests.StableReads;

/// <summary>
/// A store for keyset tests (FDW-4911) whose own order changes on every read, as a heap or a remote with no stable order does. It applies
/// a query's filter (And/Or groups of Equal and GreaterThan), its ordering, and its paging, the way a store that honours them does.
/// </summary>
internal sealed class ShufflingStore
{
    private readonly IReadOnlyList<Dictionary<string, object?>> _rows;
    private readonly bool _compare;

    /// <summary>Initializes a new instance of the <see cref="ShufflingStore"/> class.</summary>
    /// <param name="rows">The rows it holds.</param>
    /// <param name="compare">False for a store that keeps its rows in the order given and applies no filter or ordering.</param>
    public ShufflingStore(IReadOnlyList<Dictionary<string, object?>> rows, bool compare = true)
    {
        _rows = rows;
        _compare = compare;
    }

    /// <summary>Gets how many reads it answered.</summary>
    public int Reads { get; private set; }

    /// <summary>Gets the ordering each read asked for, as the field names joined by commas.</summary>
    public List<string> Orderings { get; } = [];

    /// <summary>Gets the commands it was asked.</summary>
    public List<IDataCommand> Commands { get; } = [];

    /// <summary>Answers one read.</summary>
    /// <param name="command">The query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The rows the query asks for.</returns>
    public Task<IGenericResult<IEnumerable<Dictionary<string, object?>>>> Read(IDataCommand command, CancellationToken cancellationToken)
    {
        var query = (IQueryCommand)command;
        Commands.Add(command);
        Reads++;
        Orderings.Add(query.Ordering is { } stated ? string.Join(",", stated.OrderedFields.Select(field => field.PropertyName)) : string.Empty);

        IEnumerable<Dictionary<string, object?>> rows = _rows;
        if (_compare)
        {
            rows = rows.Where(row => query.Filter?.Root is not { } root || Matches(root, row));
            rows = query.Ordering is { OrderedFields.Count: > 0 } ordering
                ? Order(rows, ordering)
                : rows.OrderBy(_ => Random.Shared.Next());
        }

        if (query.Paging is { } paging)
            rows = rows.Skip(paging.Skip).Take(paging.Take ?? int.MaxValue);

        return Task.FromResult<IGenericResult<IEnumerable<Dictionary<string, object?>>>>(
            GenericResult<IEnumerable<Dictionary<string, object?>>>.Success(rows.ToList()));
    }

    private static IOrderedEnumerable<Dictionary<string, object?>> Order(IEnumerable<Dictionary<string, object?>> rows, IOrderingExpression ordering)
    {
        var fields = ordering.OrderedFields;
        var ordered = rows.OrderBy(row => row[fields[0].PropertyName], Comparer<object?>.Default);
        for (var i = 1; i < fields.Count; i++)
        {
            var name = fields[i].PropertyName;
            ordered = ordered.ThenBy(row => row[name], Comparer<object?>.Default);
        }

        return ordered;
    }

    private static bool Matches(IFilterNode node, Dictionary<string, object?> row) => node switch
    {
        Fdw.Data.FilterGroup group when ReferenceEquals(group.Operator, LogicalOperator.And) => group.Nodes.All(child => Matches(child, row)),
        Fdw.Data.FilterGroup group => group.Nodes.Any(child => Matches(child, row)),
        FilterCondition condition => Compare(condition, row),
        _ => throw new InvalidOperationException($"The test store does not read a {node.GetType().Name}."),
    };

    private static bool Compare(FilterCondition condition, Dictionary<string, object?> row)
    {
        if (row[condition.PropertyName] is not { } value)
            return false;

        var compared = Comparer<object?>.Default.Compare(value, condition.Value);
        return condition.Operator.Name switch
        {
            "Equal" => compared == 0,
            "GreaterThan" => compared > 0,
            _ => throw new InvalidOperationException($"The test store does not read the operator {condition.Operator.Name}."),
        };
    }
}
