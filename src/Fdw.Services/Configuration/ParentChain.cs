using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Configuration.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Configuration;

/// <summary>
/// Reads the names a row sits under by following its keys up the chain of rows it hangs from, one lookup of a few keys per step.
/// </summary>
/// <remarks>
/// A page of fields names a few dozen parents at most, so each step reads a handful of rows by their own key and the cost is the page's,
/// whatever the number of fields. The rows are read by physical key, because a current field points at the version of its parent that
/// was current when it was written. A parent the caller cannot read, or that is not held, drops the field that points at it.
/// </remarks>
public sealed class ParentChain
{
    /// <summary>The most keys one lookup carries: each is a parameter of the statement.</summary>
    public const int MaxKeys = 1000;

    private static readonly string[] ByRowId = ["RowId"];

    private readonly Func<string, ConfigurationRowReader> _readers;
    private readonly IReadOnlyList<RowHop> _hops;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="ParentChain"/> class.</summary>
    /// <param name="readers">Gives the reader of a configuration container by its name.</param>
    /// <param name="hops">The steps from a row up to the rows it hangs from, in order.</param>
    /// <param name="logger">The logger.</param>
    public ParentChain(Func<string, ConfigurationRowReader> readers, IReadOnlyList<RowHop> hops, ILogger logger)
    {
        _readers = readers;
        _hops = hops;
        _logger = logger;
    }

    /// <summary>Reads the names each start key sits under.</summary>
    /// <param name="startKeys">The keys of the first step's rows.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>For each start key whose chain could be followed, the names outermost first; a key whose chain could not is absent.</returns>
    public async Task<IGenericResult<IReadOnlyDictionary<long, IReadOnlyList<string>>>> Names(
        IReadOnlyCollection<long> startKeys, CancellationToken cancellationToken)
    {
        // Each start key keeps the key it is at, and the names collected so far, innermost first.
        var at = startKeys.Distinct().ToDictionary(key => key, key => key);
        var names = at.Keys.ToDictionary(key => key, _ => new List<string>());

        foreach (var hop in _hops)
        {
            var wanted = at.Values.Distinct().ToList();
            if (wanted.Count == 0)
            {
                break;
            }

            if (wanted.Count > MaxKeys)
            {
                return GenericResult<IReadOnlyDictionary<long, IReadOnlyList<string>>>.Failure(
                    ConfigurationRowReaderLog.LookupRefused(_logger, hop.Container, wanted.Count, MaxKeys));
            }

            var read = await _readers(hop.Container).Page(
                new FilterCondition { PropertyName = "RowId", Operator = FilterOperators.ByName("In"), Value = wanted.Cast<object?>().ToList() },
                ByRowId,
                0,
                wanted.Count,
                false,
                cancellationToken).ConfigureAwait(false);
            if (read.IsFailure)
            {
                return read.ToNewResult<IReadOnlyDictionary<long, IReadOnlyList<string>>>();
            }

            ConfigurationRowReaderLog.HopRead(_logger, hop.Container, wanted.Count);
            Step(hop, read.Value!, at, names);
        }

        return GenericResult<IReadOnlyDictionary<long, IReadOnlyList<string>>>.Success(
            at.Keys.ToDictionary(key => key, key => (IReadOnlyList<string>)Enumerable.Reverse(names[key]).ToList()));
    }

    // Moves every start key one row up: collects the name the row holds and the key it points on to. A key whose row is not there is dropped.
    private void Step(RowHop hop, IReadOnlyList<ConfigurationRow> rows, Dictionary<long, long> at, Dictionary<long, List<string>> names)
    {
        var byKey = rows.Where(row => row.Number("RowId") is not null).ToDictionary(row => row.Number("RowId")!.Value);
        foreach (var start in at.Keys.ToList())
        {
            if (!byKey.TryGetValue(at[start], out var row))
            {
                ConfigurationRowReaderLog.RowNotReadable(_logger, hop.Container, at[start]);
                at.Remove(start);
                names.Remove(start);
                continue;
            }

            if (hop.NameColumn is not null)
            {
                names[start].Add(row.Text(hop.NameColumn) ?? string.Empty);
            }

            if (hop.NextColumn is not null)
            {
                at[start] = row.Number(hop.NextColumn) ?? throw new InvalidOperationException($"Row {at[start]} of {hop.Container} has no value in '{hop.NextColumn}'");
            }
        }
    }
}
