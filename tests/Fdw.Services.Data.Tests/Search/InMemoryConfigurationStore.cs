using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Moq;

namespace Fdw.Services.Data.Tests.Search;

/// <summary>
/// A configuration store that holds tables of rows in memory and answers the queries the search issues the way the database would:
/// filter (equal, in, starts with, contains, null tests, and/or), order by columns, page, and count. It keeps what it was asked and how
/// many rows it handed back, so a test can say the cost of a search.
/// </summary>
internal sealed class InMemoryConfigurationStore
{
    private readonly Dictionary<string, List<Dictionary<string, object?>>> _tables = new(StringComparer.Ordinal);

    public InMemoryConfigurationStore()
    {
        var gateway = new Mock<IConfigurationGateway>();
        gateway
            .Setup(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IDataCommand command, DataStoreTarget target, CancellationToken _) =>
                GenericResult<IEnumerable<Dictionary<string, object?>>>.Success(Answer((QueryCommand<Dictionary<string, object?>>)command, target)));
        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways.Setup(p => p.Get(It.IsAny<string>())).Returns(GenericResult<IConfigurationGateway>.Success(gateway.Object));
        Gateways = gateways.Object;
    }

    /// <summary>Gets the provider the readers under test are given.</summary>
    public IConfigurationGatewayProvider Gateways { get; }

    /// <summary>Gets the containers asked of, one entry per query, in order.</summary>
    public List<string> Queries { get; } = [];

    /// <summary>Gets the queries asked of, in order, so a test can read what each carried.</summary>
    public List<QueryCommand<Dictionary<string, object?>>> Commands { get; } = [];

    /// <summary>Gets how many rows the store handed back over all queries.</summary>
    public int RowsReturned { get; private set; }

    /// <summary>Puts rows in a container, each current and not deleted unless it says otherwise.</summary>
    public InMemoryConfigurationStore Holds(string container, IEnumerable<Dictionary<string, object?>> rows)
    {
        _tables[container] = rows
            .Select(row =>
            {
                var complete = new Dictionary<string, object?>(row, StringComparer.OrdinalIgnoreCase);
                complete.TryAdd("IsCurrent", true);
                complete.TryAdd("IsDeleted", false);
                return complete;
            })
            .ToList();
        return this;
    }

    /// <summary>Makes a row of columns.</summary>
    public static Dictionary<string, object?> Row(params (string Column, object? Value)[] columns)
        => columns.ToDictionary(column => column.Column, column => column.Value, StringComparer.OrdinalIgnoreCase);

    private List<Dictionary<string, object?>> Answer(QueryCommand<Dictionary<string, object?>> query, DataStoreTarget target)
    {
        Queries.Add(target.Container);
        Commands.Add(query);
        var selected = (_tables.TryGetValue(target.Container, out var table) ? table : [])
            .Where(row => query.Filter?.Root is not { } root || Matches(root, row))
            .ToList();

        if (query.Aggregation is not null)
        {
            return [new Dictionary<string, object?>(StringComparer.Ordinal) { ["Total"] = selected.Count }];
        }

        IEnumerable<Dictionary<string, object?>> ordered = selected;
        if (query.Ordering is { } ordering && ordering.OrderedFields.Count > 0)
        {
            var sorted = ordered.OrderBy(row => 0);
            foreach (var field in ordering.OrderedFields)
            {
                sorted = sorted.ThenBy(row => row[field.PropertyName], new ColumnComparer());
            }

            ordered = sorted;
        }

        if (query.Paging is { } paging)
        {
            ordered = ordered.Skip(paging.Skip);
            if (paging.Take is { } take)
            {
                ordered = ordered.Take(take);
            }
        }

        var page = ordered.ToList();
        RowsReturned += page.Count;
        return page;
    }

    private static bool Matches(IFilterNode node, Dictionary<string, object?> row)
    {
        switch (node)
        {
            case FilterGroup group:
                return string.Equals(group.Operator.Name, "And", StringComparison.Ordinal) ? group.Nodes.All(child => Matches(child, row)) : group.Nodes.Any(child => Matches(child, row));
            case FilterCondition condition:
                var value = row[condition.PropertyName];
                var text = value?.ToString() ?? string.Empty;
                return condition.Operator.Name switch
                {
                    "Equal" => value is not null && (value is string ? string.Equals(text, condition.Value?.ToString(), StringComparison.OrdinalIgnoreCase) : Equals(value, condition.Value)),
                    "StartsWith" => value is not null && text.StartsWith(condition.Value!.ToString()!, StringComparison.OrdinalIgnoreCase),
                    "Contains" => value is not null && text.Contains(condition.Value!.ToString()!, StringComparison.OrdinalIgnoreCase),
                    "In" => value is not null && ((IEnumerable)condition.Value!).Cast<object?>().Any(item => Same(item, value)),
                    "IsNull" => value is null,
                    "IsNotNull" => value is not null,
                    _ => throw new NotSupportedException($"The fake store does not answer operator {condition.Operator.Name}"),
                };
            default:
                throw new NotSupportedException($"The fake store does not answer a {node.GetType().Name}");
        }
    }

    // A key in a list is a whole number compared as one; any other value (an identifier, text) is compared as text without regard to case.
    private static bool Same(object? item, object value)
        => value is int or long
            ? Convert.ToInt64(item, CultureInfo.InvariantCulture) == Convert.ToInt64(value, CultureInfo.InvariantCulture)
            : string.Equals(item?.ToString(), value.ToString(), StringComparison.OrdinalIgnoreCase);

    private sealed class ColumnComparer : IComparer<object?>
    {
        public int Compare(object? left, object? right) => (left, right) switch
        {
            (null, null) => 0,
            (null, _) => -1,
            (_, null) => 1,
            (string a, string b) => StringComparer.OrdinalIgnoreCase.Compare(a, b),
            _ => Comparer<long>.Default.Compare(Convert.ToInt64(left, CultureInfo.InvariantCulture), Convert.ToInt64(right, CultureInfo.InvariantCulture)),
        };
    }
}
