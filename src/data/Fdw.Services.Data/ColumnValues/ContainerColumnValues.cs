using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Abstractions.ColumnValues;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.ColumnValues;

/// <summary>
/// Searches the distinct values of a container column through the data gateway (FDW-5666).
/// </summary>
/// <remarks>
/// One grouped read does the work on the store: the column grouped with a row count per value, a contains match on the text as
/// the person typed it (the translator escapes it and binds it as a parameter), no null value, value order, and skip with
/// take + 1, so the extra value says whether there is more. The column is checked against the container's discovered fields
/// before the store is asked, and a request the service refuses never reaches it.
/// </remarks>
public sealed class ContainerColumnValues : IContainerColumnValues
{
    /// <summary>The name the grouped read gives each value's row count.</summary>
    public const string RowsColumn = "FdwValueRows";

    private readonly ILogger<ContainerColumnValues> _logger;
    private readonly IDataGatewayProvider _gateways;
    private readonly IDataStoreProvider _stores;
    private readonly ColumnValuesPaging _paging;

    /// <summary>Initializes a new instance of the <see cref="ContainerColumnValues"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="gateways">Supplies the gateway the read is sent through.</param>
    /// <param name="stores">Builds the container, whose discovered fields the column is checked against.</param>
    /// <param name="paging">The page sizes the Main data gateway option declares.</param>
    public ContainerColumnValues(ILogger<ContainerColumnValues>? logger, IDataGatewayProvider gateways, IDataStoreProvider stores, ColumnValuesPaging paging)
    {
        _logger = logger ?? NullLogger<ContainerColumnValues>.Instance;
        _gateways = gateways;
        _stores = stores;
        _paging = paging;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<ColumnValuesPage>> Search(ColumnValuesQuery query, CancellationToken cancellationToken = default)
    {
        var take = query.Take ?? _paging.DefaultTake;
        if (Refusal(query, take) is { } refused)
            return GenericResult<ColumnValuesPage>.Failure(refused);

        ColumnValuesLog.Searching(_logger, query.Column, query.Container, query.DataStore, query.Skip, take);

        var container = await _stores.Get(query.DataStore, query.Path, query.Container, cancellationToken).ConfigureAwait(false);
        if (container.IsFailure || container.Value is null)
        {
            ColumnValuesLog.ContainerUnreachable(_logger, query.Container, query.Path, query.DataStore);
            return container.ToNewResult<ColumnValuesPage>();
        }

        if (!container.Value.Schema.Fields.Any(field => string.Equals(field.Name, query.Column, StringComparison.OrdinalIgnoreCase)))
            return GenericResult<ColumnValuesPage>.Failure(ColumnValuesLog.ColumnNotDiscovered(_logger, query.Column, query.Container, query.DataStore));

        var rows = await _gateways.ByName(DataGatewayNames.Main)
            .Execute<IEnumerable<Dictionary<string, object?>>>(Command(query, take), new DataStoreTarget(query.DataStore, query.Path, query.Container), cancellationToken)
            .ConfigureAwait(false);
        if (rows.IsFailure || rows.Value is null)
        {
            ColumnValuesLog.ValuesUnreadable(_logger, query.Column, query.Container, query.DataStore);
            return rows.ToNewResult<ColumnValuesPage>();
        }

        return Page(query, take, [.. rows.Value]);
    }

    private IGenericMessage? Refusal(ColumnValuesQuery query, int take)
    {
        if (take < 1)
            return ColumnValuesLog.TakeNotPositive(_logger, take);

        if (take > _paging.MaximumTake)
            return ColumnValuesLog.TakeAboveMaximum(_logger, take, _paging.MaximumTake);

        if (query.Skip < 0)
            return ColumnValuesLog.SkipNegative(_logger, query.Skip);

        return query.Chosen.Any(string.IsNullOrWhiteSpace) ? ColumnValuesLog.ChosenValueEmpty(_logger, query.Column) : null;
    }

    private static QueryCommand<Dictionary<string, object?>> Command(ColumnValuesQuery query, int take)
        => new()
        {
            Filter = new FilterExpression { Root = new FilterGroup { Operator = LogicalOperator.And, Nodes = Conditions(query) } },
            Ordering = new OrderingExpression { OrderedFields = [new OrderedField { PropertyName = query.Column, Direction = SortDirections.Ascending }] },
            Paging = new PagingExpression { Skip = query.Skip, Take = take + 1 },
            Aggregation = new AggregationExpression
            {
                GroupByFields = [query.Column],
                Aggregations = new Dictionary<string, string>(StringComparer.Ordinal) { [RowsColumn] = "COUNT(*)" },
            },
        };

    private static List<IFilterNode> Conditions(ColumnValuesQuery query)
    {
        List<IFilterNode> conditions = [new FilterCondition { PropertyName = query.Column, Operator = FilterOperators.ByName("IsNotNull") }];

        if (!string.IsNullOrEmpty(query.Search))
            conditions.Add(new FilterCondition { PropertyName = query.Column, Operator = FilterOperators.ByName("Contains"), Value = query.Search });

        if (query.LeaveOutChosen && query.Chosen.Count > 0)
            conditions.Add(new FilterCondition { PropertyName = query.Column, Operator = FilterOperators.ByName("NotIn"), Value = query.Chosen.Cast<object?>().ToList() });

        return conditions;
    }

    private IGenericResult<ColumnValuesPage> Page(ColumnValuesQuery query, int take, List<Dictionary<string, object?>> rows)
    {
        var chosen = new HashSet<string>(query.Chosen, StringComparer.Ordinal);
        var items = new List<ColumnValue>();
        foreach (var row in rows.Take(take))
        {
            if (!TryCount(row, out var count))
                return GenericResult<ColumnValuesPage>.Failure(ColumnValuesLog.RowCountNotWhole(_logger, query.Column));

            var value = Convert.ToString(Value(row, query.Column), CultureInfo.InvariantCulture) ?? string.Empty;
            items.Add(new ColumnValue { Value = value, Rows = count, Chosen = chosen.Contains(value) });
        }

        var hasMore = rows.Count > take;
        ColumnValuesLog.Searched(_logger, items.Count, query.Column, query.Container, hasMore);
        return GenericResult<ColumnValuesPage>.Success(new ColumnValuesPage { Items = items, HasMore = hasMore });
    }

    // The row is keyed by the column's own name as the store answers it, which may differ in case from the request's.
    private static object? Value(Dictionary<string, object?> row, string column)
        => row.FirstOrDefault(entry => string.Equals(entry.Key, column, StringComparison.OrdinalIgnoreCase)).Value;

    private static bool TryCount(Dictionary<string, object?> row, out long count)
    {
        switch (Value(row, RowsColumn))
        {
            case int value:
                count = value;
                return true;
            case long value:
                count = value;
                return true;
            default:
                count = 0;
                return false;
        }
    }
}
