using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data;

/// <summary>
/// Reads rows page by page with Skip/Take in the order the store itself keeps, for a store whose connection declares that it keeps one
/// (<c>IStoreOrderedConnection</c>; FDW-4911): a remote API paged by its protocol, a file read in its row order.
/// </summary>
/// <remarks>
/// The ordering guarantee is the store's, stated by its connection; nothing here assumes one for a store that did not state it. The read
/// ends at a page shorter than the page size. A store that does not page answers its whole set to the first ask, more rows than a page,
/// and the read ends there. A full page equal, row for row, to the page before it stops the read with <c>StoreIgnoredSkip</c>: the store
/// ignores Skip and would answer that page for ever, or the data holds two full pages of identical rows, which pages by position cannot
/// tell apart. Only the page before is held for that check. Pages are read with commands of its own, which never ask for the result cache;
/// the caller's filter, projection and field selection apply to every page.
/// </remarks>
public sealed class StoreOrderedRecordSource : IRecordSource<DataRecord>
{
    private readonly Func<IDataCommand, CancellationToken, Task<IGenericResult<IEnumerable<Dictionary<string, object?>>>>> _readPage;
    private readonly IQueryCommand _query;
    private readonly int _pageSize;
    private readonly string _target;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="StoreOrderedRecordSource"/> class.</summary>
    /// <param name="schema">The fields every record carries; a field a row lacks is null in its record.</param>
    /// <param name="readPage">Reads one page: runs the page's command and answers its rows, each keyed by field name.</param>
    /// <param name="query">The caller's query; its filter, projection and field selection apply to every page. Its own paging is not used.</param>
    /// <param name="pageSize">How many rows a page asks for; at least one.</param>
    /// <param name="target">What is read, for the messages: a container of a store.</param>
    /// <param name="logger">The logger; null for none.</param>
    public StoreOrderedRecordSource(
        RecordSchema schema,
        Func<IDataCommand, CancellationToken, Task<IGenericResult<IEnumerable<Dictionary<string, object?>>>>> readPage,
        IQueryCommand query,
        int pageSize,
        string target,
        ILogger? logger = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentException.ThrowIfNullOrWhiteSpace(target);

        Schema = schema;
        _readPage = readPage;
        _query = query;
        _pageSize = pageSize;
        _target = target;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <inheritdoc />
    public RecordSchema Schema { get; }

    /// <inheritdoc />
    public IEnumerable<IGenericResult<DataRecord>> Read() => Read(CancellationToken.None).ToBlockingEnumerable();

    /// <inheritdoc />
    public async IAsyncEnumerable<IGenericResult<DataRecord>> Read([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var skip = 0;
        List<Dictionary<string, object?>> previous = [];
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var page = await _readPage(PageCommand(skip), cancellationToken).ConfigureAwait(false);
            if (page.IsFailure)
            {
                yield return page.ToNewResult<DataRecord>();
                yield break;
            }

            var rows = page.Value is { } read ? read.ToList() : [];
            if (SamePage(previous, rows))
            {
                yield return GenericResult<DataRecord>.Failure(
                    DataServiceResultCodes.ByName("StoreIgnoredSkip"), _logger,
                    ResultDetails.Create("target", _target, "skip", skip));
                yield break;
            }

            foreach (var row in rows)
                yield return GenericResult<DataRecord>.Success(RowListRecordSource.Record(Schema, row));

            // Why both ends stop: a short page is the last one, and more rows than a page is a store that answered its whole set.
            if (rows.Count != _pageSize)
                yield break;

            skip += rows.Count;
            previous = rows;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private QueryCommand<Dictionary<string, object?>> PageCommand(int skip) =>
        new()
        {
            Filter = _query.Filter,
            Projection = _query.Projection,
            IncludeMetadata = _query.IncludeMetadata,
            IncludeDescriptions = _query.IncludeDescriptions,
            IncludeKeys = _query.IncludeKeys,
            Ordering = _query.Ordering,
            Paging = new PagingExpression { Skip = skip, Take = _pageSize },
        };

    // The page before is held only when it was full, so an equal page is full too: the same rows, in the same order, with the same values.
    private static bool SamePage(List<Dictionary<string, object?>> previous, List<Dictionary<string, object?>> rows) =>
        previous.Count > 0 && previous.Count == rows.Count && previous.Zip(rows).All(pair => SameRow(pair.First, pair.Second));

    private static bool SameRow(Dictionary<string, object?> left, Dictionary<string, object?> right) =>
        left.Count == right.Count && left.All(field => right.TryGetValue(field.Key, out var value) && Equals(field.Value, value));
}
