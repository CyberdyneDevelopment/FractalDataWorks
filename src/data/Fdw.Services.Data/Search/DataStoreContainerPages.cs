using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Configuration.Logging;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.Search;

/// <summary>
/// Reads the containers of all data stores one page at a time (FDW-2311): the page's rows of data.DataContainer in container-name order, and then the
/// path and the store each of them sits under, by reading the rows above the page only.
/// </summary>
/// <remarks>
/// A container that the caller cannot read the path or store of is left out of the page, as a field is in the field searches. The page asks for one row
/// more than it holds, which says whether another page follows.
/// </remarks>
internal sealed class DataStoreContainerPages : IDataStoreContainerPages
{
    private const string Container = "DataContainer";

    private static readonly string[] ByName = ["Name", "RowId"];

    private readonly Func<string, ConfigurationRowReader> _readers;
    private readonly ILogger _logger;

    public DataStoreContainerPages(IConfigurationGatewayProvider gateways, string dataStoreName, string pathName, ILogger<DataStoreContainerPages>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(gateways);
        _logger = logger ?? NullLogger<DataStoreContainerPages>.Instance;
        _readers = name => new ConfigurationRowReader(gateways, dataStoreName, pathName, name, _logger);
    }

    public int MaxTake => ParentChain.MaxKeys;

    public async Task<IGenericResult<DataStoreContainerPage>> Page(
        IFilterNode? condition, int skip, int take, bool includeTotal, CancellationToken cancellationToken = default)
    {
        if (take < 1 || take > MaxTake || skip < 0)
        {
            return GenericResult<DataStoreContainerPage>.Failure(ConfigurationRowReaderLog.WindowRefused(_logger, Container, take, MaxTake));
        }

        var reader = _readers(Container);
        var rows = await reader.Page(condition, ByName, skip, take + 1, true, cancellationToken).ConfigureAwait(false);
        if (rows.IsFailure)
        {
            return rows.ToNewResult<DataStoreContainerPage>();
        }

        var page = rows.Value!.Take(take).ToList();
        var chain = new ParentChain(_readers, Above, _logger);
        var paths = page.Select(row => row.Number("DataPathRowId")).Where(key => key is not null).Select(key => key!.Value).ToList();
        var names = await chain.Names(paths, cancellationToken).ConfigureAwait(false);
        if (names.IsFailure)
        {
            return names.ToNewResult<DataStoreContainerPage>();
        }

        var entries = new List<DataStoreContainerEntry>();
        foreach (var row in page)
        {
            if (row.Number("DataPathRowId") is not { } path || !names.Value!.TryGetValue(path, out var above))
            {
                continue;
            }

            var entry = EntryOf(row, above);
            if (entry is null)
            {
                return GenericResult<DataStoreContainerPage>.Failure(DataStoreContainerPagesLog.ColumnMissing(_logger, "Id, Name, TypeId or ClassificationId"));
            }

            entries.Add(entry);
        }

        int? total = null;
        if (includeTotal)
        {
            var counted = await reader.Count(condition, cancellationToken).ConfigureAwait(false);
            if (counted.IsFailure)
            {
                return counted.ToNewResult<DataStoreContainerPage>();
            }

            total = counted.Value;
        }

        DataStoreContainerPagesLog.Paged(_logger, entries.Count, skip, take);
        return GenericResult<DataStoreContainerPage>.Success(new DataStoreContainerPage(entries, rows.Value!.Count > take, total));
    }

    // From a container's path up to the store: the path (named), the store's configuration row (which only points on), and the store (named).
    private static IReadOnlyList<RowHop> Above { get; } =
    [
        new RowHop("DataPath", "Name", "DataStoreConfigurationRowId"),
        new RowHop("DataStoreConfiguration", null, "DataStoreRowId"),
        new RowHop("DataStore", "Name", null),
    ];

    // The entry of a container row and the names above it, outermost first: the store, then the path. None when the row lacks a column it needs.
    private static DataStoreContainerEntry? EntryOf(ConfigurationRow row, IReadOnlyList<string> above)
    {
        if (!Guid.TryParse(row.Text("Id"), out var id) || row.Text("Name") is not { } name || row.Text("TypeId") is not { } type
            || row.Number("ClassificationId") is not { } classification || above.Count != 2)
        {
            return null;
        }

        return new DataStoreContainerEntry(id, above[0], above[1], name, type, checked((int)classification));
    }
}
