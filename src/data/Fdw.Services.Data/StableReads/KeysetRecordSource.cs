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
/// Reads rows page by page in the order of a key, each page asking for the rows after the last key the one before ended on (FDW-4911).
/// </summary>
/// <remarks>
/// <para>
/// Offset pages ask for "rows 1000 to 1999" of whatever order the store picks, so a store with no stable order can hand back a row twice or
/// not at all, and each page costs the store every row before it. A keyset page states its order (every key field, ascending) and asks for
/// the rows after the last key, so no row is repeated or skipped and every page costs the same.
/// </para>
/// <para>
/// The read ends at a page shorter than the page size. A store that does not page answers its whole set to the first ask, more rows than a
/// page, and the read ends there: that one answer is the whole set. Keyset pages never share a key, so a page that repeats any key of the
/// page before it is a store that did not apply the filter, and the read stops with <c>KeysetFilterIgnored</c> rather than repeat rows for
/// ever. The check holds one page of keys, never the whole read. A row of a full page, or of any page after the first, with no value for a
/// key field stops it with <c>KeysetKeyValueMissing</c>. Pages are read with commands of its own, which never ask for the result cache.
/// </para>
/// </remarks>
public sealed class KeysetRecordSource : IRecordSource<DataRecord>
{
    private readonly Func<IDataCommand, CancellationToken, Task<IGenericResult<IEnumerable<Dictionary<string, object?>>>>> _readPage;
    private readonly IQueryCommand _query;
    private readonly IReadOnlyList<string> _keyFields;
    private readonly bool _includeKeys;
    private readonly int _pageSize;
    private readonly string _target;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="KeysetRecordSource"/> class.</summary>
    /// <param name="schema">The fields every record carries; a field a row lacks is null in its record.</param>
    /// <param name="readPage">Reads one page: runs the page's command and answers its rows, each keyed by field name.</param>
    /// <param name="query">The caller's query; its filter, projection and field selection apply to every page. Its own ordering and paging are not used.</param>
    /// <param name="keyFields">The fields of the key the pages follow, in order; each row must carry them.</param>
    /// <param name="includeKeys">Whether the pages ask for key fields the caller's query would leave out, so each row carries the key it is read after.</param>
    /// <param name="pageSize">How many rows a page asks for; at least one.</param>
    /// <param name="target">What is read, for the messages: a container of a store, or a data set.</param>
    /// <param name="logger">The logger; null for none.</param>
    public KeysetRecordSource(
        RecordSchema schema,
        Func<IDataCommand, CancellationToken, Task<IGenericResult<IEnumerable<Dictionary<string, object?>>>>> readPage,
        IQueryCommand query,
        IReadOnlyList<string> keyFields,
        bool includeKeys,
        int pageSize,
        string target,
        ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(readPage);
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(keyFields);
        ArgumentOutOfRangeException.ThrowIfLessThan(keyFields.Count, 1, nameof(keyFields));
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentException.ThrowIfNullOrWhiteSpace(target);

        Schema = schema;
        _readPage = readPage;
        _query = query;
        _keyFields = keyFields;
        _includeKeys = includeKeys;
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
        IReadOnlyList<object?>? lastKey = null;
        var previousKeys = new HashSet<IReadOnlyList<object?>>(KeyComparer.Instance);
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var page = await _readPage(PageCommand(lastKey), cancellationToken).ConfigureAwait(false);
            if (page.IsFailure)
            {
                yield return page.ToNewResult<DataRecord>();
                yield break;
            }

            var rows = page.Value is { } read ? read.ToList() : [];
            if (rows.Count == 0)
                yield break;

            var keys = PageKeys(rows, previousKeys);
            if (keys.IsFailure || keys.Value is not { } checkedPage)
            {
                yield return keys.ToNewResult<DataRecord>();
                yield break;
            }

            foreach (var row in rows)
                yield return GenericResult<DataRecord>.Success(RowListRecordSource.Record(Schema, row));

            // Why both ends stop: a short page is the last one, and more rows than a page is a store that answered its whole set.
            if (rows.Count != _pageSize || checkedPage.Last is not { } key)
                yield break;

            lastKey = key;
            previousKeys = checkedPage.Keys;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private QueryCommand<Dictionary<string, object?>> PageCommand(IReadOnlyList<object?>? lastKey) =>
        new()
        {
            Filter = KeysetPages.Filter(_query.Filter, _keyFields, lastKey),
            Projection = _query.Projection,
            IncludeMetadata = _query.IncludeMetadata,
            IncludeDescriptions = _query.IncludeDescriptions,
            IncludeKeys = _query.IncludeKeys || _includeKeys,
            Ordering = KeysetPages.Order(_keyFields),
            Paging = new PagingExpression { Skip = 0, Take = _pageSize },
        };

    // The keys of a page, checked against the page before it. Keyset pages never share a key, so a shared one is a store that ignored the
    // filter: answering the same page again, or starting at the key the last page ended on. The keys of the first page are read only when it
    // is full, since a short or whole-set first page is the end of the read.
    private IGenericResult<PageKeySet> PageKeys(List<Dictionary<string, object?>> rows, HashSet<IReadOnlyList<object?>> previousKeys)
    {
        var keys = new HashSet<IReadOnlyList<object?>>(KeyComparer.Instance);
        if (previousKeys.Count == 0 && rows.Count != _pageSize)
            return GenericResult<PageKeySet>.Success(new PageKeySet(keys, null));

        IReadOnlyList<object?>? last = null;
        foreach (var row in rows)
        {
            var key = KeyOf(row);
            if (key.IsFailure || key.Value is not { } value)
                return key.ToNewResult<PageKeySet>();

            if (previousKeys.Contains(value))
                return GenericResult<PageKeySet>.Failure(
                    DataServiceResultCodes.ByName("KeysetFilterIgnored"), _logger,
                    ResultDetails.Create("target", _target, "key", string.Join(", ", value)));

            keys.Add(value);
            last = value;
        }

        return GenericResult<PageKeySet>.Success(new PageKeySet(keys, last));
    }

    private IGenericResult<IReadOnlyList<object?>> KeyOf(Dictionary<string, object?> row)
    {
        var key = new object?[_keyFields.Count];
        for (var i = 0; i < key.Length; i++)
        {
            if (!row.TryGetValue(_keyFields[i], out var value) || value is null || value is DBNull)
                return GenericResult<IReadOnlyList<object?>>.Failure(
                    DataServiceResultCodes.ByName("KeysetKeyValueMissing"), _logger,
                    ResultDetails.Create("target", _target, "field", _keyFields[i]));

            key[i] = value;
        }

        return GenericResult<IReadOnlyList<object?>>.Success(key);
    }

    // The keys of one page, kept for the check on the next, and the key the page ended on, which the next page reads after.
    private sealed record PageKeySet(HashSet<IReadOnlyList<object?>> Keys, IReadOnlyList<object?>? Last);

    // Two keys are the same when every field value is equal, in order.
    private sealed class KeyComparer : IEqualityComparer<IReadOnlyList<object?>>
    {
        public static readonly KeyComparer Instance = new();

        public bool Equals(IReadOnlyList<object?>? x, IReadOnlyList<object?>? y) =>
            ReferenceEquals(x, y) || (x is not null && y is not null && x.SequenceEqual(y));

        public int GetHashCode(IReadOnlyList<object?> obj)
        {
            ArgumentNullException.ThrowIfNull(obj);
            var hash = default(HashCode);
            foreach (var value in obj)
                hash.Add(value);

            return hash.ToHashCode();
        }
    }
}
