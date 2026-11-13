using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data;

/// <summary>
/// Judges, when a Graph data set is read, the data sets it reads through each other (FDW-5299): a cycle is refused and so is a nesting deeper than the stated depth.
/// </summary>
/// <remarks>
/// <para>
/// The walk is story 2's <see cref="GraphDataSetCycles.Find"/>, the one cycle check, here over the stored definitions: the start's reads are what the data set read holds now and the
/// other data sets are read through <see cref="IGraphDataSetSources"/>, one batched read per level. So a cycle made after the data set was saved, or one saving could not see, is met
/// here by name and never followed.
/// </para>
/// <para>
/// It holds no depth of its own: the caller states how many levels may nest, from configuration, and a value that is not a positive number is the caller's refusal to make.
/// </para>
/// </remarks>
public static class GraphDataSetNesting
{
    /// <summary>Checks that a Graph data set about to be read reads no cycle and nests no deeper than stated.</summary>
    /// <param name="dataSet">The Graph data set being read, as stored.</param>
    /// <param name="sources">Reads the data sets it reads, and the ones they read in turn.</param>
    /// <param name="maxDepth">The most levels of data sets that may sit below the Graph data set; positive.</param>
    /// <param name="logger">The logger a failure is written to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success; or the cycle (DATA-21407), the depth exceeded (DATA-21411), or the failure of a read.</returns>
    public static async Task<IGenericResult> Check(
        IGraphDataSetConfiguration dataSet, IGraphDataSetSources sources, int maxDepth, ILogger logger, CancellationToken cancellationToken = default)
    {
        var found = await GraphDataSetCycles.Find(
            dataSet.Name, GraphDataSetNestedReads.Of(dataSet), (names, token) => Reads(sources, names, token), cancellationToken).ConfigureAwait(false);
        if (found.IsFailure || found.Value is not { } result)
        {
            return found;
        }

        if (result.HasCycle)
        {
            return GenericResult.Failure(
                DataServiceResultCodes.ByName("GraphDataSetCycle"), logger,
                ResultDetails.Create("dataSet", dataSet.Name, "cycle", string.Join(" -> ", result.Cycle)));
        }

        return result.Depth > maxDepth
            ? GenericResult.Failure(
                DataServiceResultCodes.ByName("GraphDataSetDepthExceeded"), logger,
                ResultDetails.Create("dataSet", dataSet.Name, "depth", result.Depth, "limit", maxDepth))
            : GenericResult.Success();
    }

    // What each named data set reads; a name the store does not hold reads nothing.
    private static async Task<IGenericResult<IReadOnlyDictionary<string, IReadOnlyList<string>>>> Reads(
        IGraphDataSetSources sources, IReadOnlyCollection<string> names, CancellationToken cancellationToken)
    {
        var read = await sources.DataSets(names, cancellationToken).ConfigureAwait(false);
        if (read.IsFailure || read.Value is null)
        {
            return read.ToNewResult<IReadOnlyDictionary<string, IReadOnlyList<string>>>();
        }

        return GenericResult<IReadOnlyDictionary<string, IReadOnlyList<string>>>.Success(
            read.Value.ToDictionary(fact => fact.Key, fact => fact.Value.Reads, StringComparer.OrdinalIgnoreCase));
    }
}
