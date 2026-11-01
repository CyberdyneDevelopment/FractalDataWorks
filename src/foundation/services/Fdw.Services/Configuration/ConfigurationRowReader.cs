using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Abstractions.Caching;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Configuration.Logging;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Configuration;

/// <summary>
/// Reads the rows of one configuration container as <see cref="ConfigurationRow"/>s, a page at a time, and counts them.
/// </summary>
/// <remarks>
/// <para>
/// For a container nobody lists whole: the fields of data sets and of containers are child rows that a domain provider reads through
/// their parent. A search over them asks the database for the rows it wants, so the cost of a read is the rows it returns.
/// </para>
/// <para>
/// A read of current rows always applies the version predicates (the row is current and not deleted) and the caller's condition narrows
/// them; it never reaches a retired or deleted row. A read by physical key (<c>current</c> false) names the rows by their own key
/// alone, for a row a current child points at.
/// </para>
/// <para>
/// A read of a view is cached under the view, and nothing writes a view, so its cached page would never be dropped. A reader over a
/// view is told the containers the view reads; the read is then cached under the view and each of them, and a write to any of them drops it.
/// </para>
/// </remarks>
public sealed class ConfigurationRowReader
{
    private const string CountColumn = ConfigurationCommandBase<DomainConfiguration>.CountColumn;

    private readonly IConfigurationGatewayProvider _gateways;
    private readonly string _dataStoreName;
    private readonly string _pathName;
    private readonly string _containerName;
    private readonly string[]? _cacheTags;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="ConfigurationRowReader"/> class.</summary>
    /// <param name="gateways">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The configuration connection the rows live on.</param>
    /// <param name="pathName">The schema the rows live in.</param>
    /// <param name="containerName">The container the rows live in.</param>
    /// <param name="logger">The logger a read is traced through.</param>
    /// <param name="alsoReads">
    /// For a view: the containers, in the same schema, whose rows the view is made of. Null for a table, which is tagged by itself.
    /// </param>
    public ConfigurationRowReader(
        IConfigurationGatewayProvider gateways, string dataStoreName, string pathName, string containerName, ILogger? logger = null,
        IReadOnlyList<string>? alsoReads = null)
    {
        ArgumentNullException.ThrowIfNull(gateways);
        ArgumentNullException.ThrowIfNull(dataStoreName);
        ArgumentNullException.ThrowIfNull(pathName);
        ArgumentNullException.ThrowIfNull(containerName);
        _gateways = gateways;
        _dataStoreName = dataStoreName;
        _pathName = pathName;
        _containerName = containerName;
        _cacheTags = alsoReads is null
            ? null
            : [.. alsoReads.Prepend(containerName).Select(container => string.Concat(pathName, ".", container))];
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>Gets the name of the container the rows live in.</summary>
    public string ContainerName => _containerName;

    /// <summary>Reads one page of the rows a condition selects.</summary>
    /// <param name="condition">Which rows, or nothing for every row.</param>
    /// <param name="orderBy">The columns to order by, most significant first, each ascending; the last should be a key, so two pages cannot overlap.</param>
    /// <param name="skip">How many selected rows precede the page.</param>
    /// <param name="take">How many rows the page holds at most.</param>
    /// <param name="current">True to read current, not deleted rows; false to read by physical key.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The rows, or the failure of the read.</returns>
    public async Task<IGenericResult<IReadOnlyList<ConfigurationRow>>> Page(
        IFilterNode? condition,
        IReadOnlyList<string> orderBy,
        int skip,
        int take,
        bool current,
        CancellationToken cancellationToken = default)
    {
        var gateway = _gateways.Get(_dataStoreName);
        if (gateway.IsFailure)
        {
            return gateway.ToNewResult<IReadOnlyList<ConfigurationRow>>();
        }

        var ascending = SortDirections.ByName("Ascending");
        var command = new QueryCommand<Dictionary<string, object?>>
        {
            Filter = Filter(condition, current),
            Ordering = new OrderingExpression
            {
                OrderedFields = orderBy.Select(column => (IOrderedField)new OrderedField { PropertyName = column, Direction = ascending }).ToList(),
            },
            Paging = new PagingExpression { Skip = skip, Take = take },
            Metadata = CacheMetadata(),
        };

        var rows = await gateway.Value!.Execute<IEnumerable<Dictionary<string, object?>>>(command, Target, cancellationToken).ConfigureAwait(false);
        if (rows.IsFailure)
        {
            return rows.ToNewResult<IReadOnlyList<ConfigurationRow>>();
        }

        if (rows.Value is null)
        {
            return GenericResult<IReadOnlyList<ConfigurationRow>>.Failure(ConfigurationRowReaderLog.PageReturnedNoResult(_logger, _containerName, skip, take));
        }

        var read = rows.Value.Select(row => new ConfigurationRow(row)).ToList();
        ConfigurationRowReaderLog.PageRead(_logger, _containerName, skip, take, read.Count);
        return GenericResult<IReadOnlyList<ConfigurationRow>>.Success(read);
    }

    /// <summary>Counts the current rows a condition selects.</summary>
    /// <param name="condition">Which rows, or nothing for every row.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The number of rows, or the failure of the read.</returns>
    public async Task<IGenericResult<int>> Count(IFilterNode? condition, CancellationToken cancellationToken = default)
    {
        var gateway = _gateways.Get(_dataStoreName);
        if (gateway.IsFailure)
        {
            return gateway.ToNewResult<int>();
        }

        var command = new QueryCommand<Dictionary<string, object?>>
        {
            Filter = Filter(condition, current: true),
            Aggregation = new AggregationExpression
            {
                GroupByFields = [],
                Aggregations = new Dictionary<string, string>(StringComparer.Ordinal) { [CountColumn] = "COUNT(*)" },
            },
            Metadata = CacheMetadata(),
        };

        var counted = await gateway.Value!.Execute<IEnumerable<Dictionary<string, object?>>>(command, Target, cancellationToken).ConfigureAwait(false);
        if (counted.IsFailure)
        {
            return counted.ToNewResult<int>();
        }

        if (counted.Value?.FirstOrDefault() is { } row && new ConfigurationRow(row).Number(CountColumn) is { } number)
        {
            ConfigurationRowReaderLog.Counted(_logger, _containerName, number);
            return GenericResult<int>.Success(checked((int)number));
        }

        return GenericResult<int>.Failure(ConfigurationRowReaderLog.CountReturnedNoResult(_logger, _containerName));
    }

    private DataStoreTarget Target => new(_dataStoreName, _pathName, _containerName);

    // Why an empty map and not null for a table: the command's metadata is never null, and a reader that was told nothing leaves the
    // gateway to tag the read by its own container, as before.
    private Dictionary<string, object> CacheMetadata()
        => _cacheTags is null
            ? []
            : new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { [CachePolicy.CacheInvalidationTagsKey] = _cacheTags };

    // The version predicates when current rows are read; the caller's condition narrows them and never widens them.
    private static FilterExpression Filter(IFilterNode? condition, bool current)
    {
        var nodes = new List<IFilterNode>();
        if (current)
        {
            nodes.Add(new FilterCondition { PropertyName = "IsCurrent", Operator = FilterOperators.ByName("Equal"), Value = true });
            nodes.Add(new FilterCondition { PropertyName = "IsDeleted", Operator = FilterOperators.ByName("Equal"), Value = false });
        }

        if (condition is not null)
        {
            nodes.Add(condition);
        }

        return nodes.Count switch
        {
            0 => new FilterExpression(),
            1 => new FilterExpression { Root = nodes[0] },
            _ => new FilterExpression { Root = new FilterGroup { Operator = LogicalOperator.And, Nodes = nodes } },
        };
    }
}
