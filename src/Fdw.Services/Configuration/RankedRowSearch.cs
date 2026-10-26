using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Configuration.Logging;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Configuration;

/// <summary>
/// Finds the rows of one container whose name or description is closest to a text, asking the database for ordered windows of rows and
/// never for the container whole.
/// </summary>
/// <remarks>
/// <para>
/// The rank is <see cref="SearchRank"/>: 0 the name is the text, 1 it starts with it, 2 it contains it, 3 only the description contains it.
/// The database is asked, ordered by name then key, for the rows whose name starts with the text (a window), whose name contains it (two
/// windows) and whose description contains it (two windows). Each row is then ranked again here and the closest window are kept.
/// </para>
/// <para>
/// Why that is exact: in the window of rows whose name starts with the text, the rows whose name is the text come first, because a name
/// equal to the text sorts before any longer name that starts with it; so when that window is full it holds the closest rows and no
/// other query is needed. When it is not full, fewer than a window of rows rank 0 or 1, and a window of twice the size of the next class
/// holds at least a window of rows of its own class, because at most that many rows of the closer classes can be in it.
/// </para>
/// <para>
/// A row the database finds that the rank rule does not (the database compares accents or width differently) ranks 3: it matched, and
/// nothing about it is closer. The count is the database's.
/// </para>
/// </remarks>
public sealed class RankedRowSearch
{
    private readonly ConfigurationRowReader _reader;
    private readonly string _nameColumn;
    private readonly string _descriptionColumn;
    private readonly string _keyColumn;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="RankedRowSearch"/> class.</summary>
    /// <param name="reader">Reads the container's rows.</param>
    /// <param name="nameColumn">The column holding the name.</param>
    /// <param name="descriptionColumn">The column holding the description.</param>
    /// <param name="keyColumn">The column holding the row's own key, which settles the order of rows with the same name.</param>
    /// <param name="logger">The logger a search is traced through.</param>
    public RankedRowSearch(ConfigurationRowReader reader, string nameColumn, string descriptionColumn, string keyColumn, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(nameColumn);
        ArgumentNullException.ThrowIfNull(descriptionColumn);
        ArgumentNullException.ThrowIfNull(keyColumn);
        _reader = reader;
        _nameColumn = nameColumn;
        _descriptionColumn = descriptionColumn;
        _keyColumn = keyColumn;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>Finds the closest rows to a text.</summary>
    /// <param name="text">What to find; empty lists the first rows by name.</param>
    /// <param name="window">How many of the closest rows to return.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The closest rows and the number of rows that match, or the failure of a read.</returns>
    public async Task<IGenericResult<RankedRows>> Search(string text, int window, CancellationToken cancellationToken = default)
    {
        var order = new[] { _nameColumn, _keyColumn };
        var found = new Dictionary<long, RankedRow>();

        var matches = text.Length == 0
            ? await Browse(order, window, found, cancellationToken).ConfigureAwait(false)
            : await Find(text, order, window, found, cancellationToken).ConfigureAwait(false);
        if (matches.IsFailure)
        {
            return matches.ToNewResult<RankedRows>();
        }

        var closest = found.Values
            .OrderBy(candidate => candidate.Rank)
            .ThenBy(candidate => candidate.Row.Text(_nameColumn), StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.Key)
            .Take(window)
            .ToList();
        ConfigurationRowReaderLog.Searched(_logger, _reader.ContainerName, text, window, found.Count, matches.Value);
        return GenericResult<RankedRows>.Success(new RankedRows(closest, matches.Value));
    }

    // No text: every row matches equally, so the first rows by name are the answer.
    private async Task<IGenericResult<int>> Browse(string[] order, int window, Dictionary<long, RankedRow> found, CancellationToken cancellationToken)
    {
        var page = await _reader.Page(null, order, 0, window, true, cancellationToken).ConfigureAwait(false);
        if (page.IsFailure)
        {
            return page.ToNewResult<int>();
        }

        Add(found, page.Value!, string.Empty);
        return await _reader.Count(null, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IGenericResult<int>> Find(string text, string[] order, int window, Dictionary<long, RankedRow> found, CancellationToken cancellationToken)
    {
        var starting = await _reader.Page(Condition(_nameColumn, "StartsWith", text), order, 0, window, true, cancellationToken).ConfigureAwait(false);
        if (starting.IsFailure)
        {
            return starting.ToNewResult<int>();
        }

        Add(found, starting.Value!, text);

        // A full window of rows that start with the text holds the closest rows; only a short one leaves room for the wider classes.
        if (starting.Value!.Count < window)
        {
            var wider = await ReadWider(text, order, window, found, cancellationToken).ConfigureAwait(false);
            if (wider.IsFailure)
            {
                return wider.ToNewResult<int>();
            }
        }

        return await _reader.Count(Either(text), cancellationToken).ConfigureAwait(false);
    }

    private async Task<IGenericResult> ReadWider(string text, string[] order, int window, Dictionary<long, RankedRow> found, CancellationToken cancellationToken)
    {
        var inName = await _reader.Page(Condition(_nameColumn, "Contains", text), order, 0, 2 * window, true, cancellationToken).ConfigureAwait(false);
        if (inName.IsFailure)
        {
            return inName;
        }

        Add(found, inName.Value!, text);

        var inDescription = await _reader.Page(Condition(_descriptionColumn, "Contains", text), order, 0, 2 * window, true, cancellationToken).ConfigureAwait(false);
        if (inDescription.IsFailure)
        {
            return inDescription;
        }

        Add(found, inDescription.Value!, text);
        return GenericResult.Success();
    }

    // Ranks each row here, by the rule every source shares, and keeps one of a row that more than one window read.
    private void Add(Dictionary<long, RankedRow> found, IReadOnlyList<ConfigurationRow> rows, string text)
    {
        foreach (var row in rows)
        {
            var key = row.Number(_keyColumn) ?? throw new InvalidOperationException($"A row of {_reader.ContainerName} has no value in its key column '{_keyColumn}'");
            var rank = SearchRank.Of(row.Text(_nameColumn) ?? string.Empty, row.Text(_descriptionColumn), text);
            found[key] = new RankedRow(row, rank == SearchRank.NoMatch ? 3 : rank, key);
        }
    }

    private static FilterCondition Condition(string column, string operatorName, string text)
        => new() { PropertyName = column, Operator = FilterOperators.ByName(operatorName), Value = text };

    // Name contains the text, or description does: the rows the text finds, whatever their rank.
    private FilterGroup Either(string text) => new()
    {
        Operator = LogicalOperator.Or,
        Nodes = [Condition(_nameColumn, "Contains", text), Condition(_descriptionColumn, "Contains", text)],
    };
}
