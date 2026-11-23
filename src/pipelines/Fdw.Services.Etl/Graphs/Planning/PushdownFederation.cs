using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fdw.Services.Etl.Graphs.Planning;

/// <summary>
/// Finds the reads that feed a join, lookup or union across data stores (FDW-6970): the combiners left in memory whose direct inputs read more than one
/// data store. Those reads run under the federation strategy's read gate; a pushed sub-graph that feeds one is a read of its own store.
/// </summary>
/// <remarks>
/// Each read's part states the store it reads (<see cref="IPushdownPart.ReadStore"/>): a container source the data store record its container is
/// described by, a data set source its data set. A read whose store cannot be judged is not counted, and the trace says so: nothing is judged by name.
/// </remarks>
public static class PushdownFederation
{
    /// <summary>Finds the federated reads of the combiners left in memory.</summary>
    /// <param name="scope">The graph.</param>
    /// <param name="inMemory">The combiners left in memory, each with its part.</param>
    /// <param name="pushed">The sub-graphs pushed into their stores.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Each read once; one that feeds a build side anywhere is a build-side read.</returns>
    public static async Task<IReadOnlyList<FederatedRead>> Reads(
        PushdownScope scope,
        IReadOnlyList<(PipelineComponentConfiguration Component, IPushdownPart Part)> inMemory,
        IReadOnlyList<PushedSubGraph> pushed,
        CancellationToken cancellationToken = default)
    {
        var found = new List<FederatedRead>();
        foreach (var (combiner, part) in inMemory)
        {
            var reads = new List<(string Read, string Store, bool BuildSide)>();
            foreach (var line in scope.Rows.Where(line => string.Equals(line.ToComponent, combiner.Name, StringComparison.OrdinalIgnoreCase)))
            {
                var store = await StoreOf(scope, line.FromComponent, pushed, cancellationToken).ConfigureAwait(false);
                if (store.Length > 0)
                {
                    reads.Add((line.FromComponent, store, part.BuildSideInput.Length > 0 && string.Equals(line.ToInput, part.BuildSideInput, StringComparison.OrdinalIgnoreCase)));
                }
            }

            if (reads.Select(read => read.Store).Distinct(StringComparer.Ordinal).Count() < 2)
            {
                continue;
            }

            PushdownLog.FederatedReadsFound(scope.Logger, scope.Graph.Name, combiner.Name, string.Join(", ", reads.Select(read => read.Read)));
            found.AddRange(reads.Select(read => new FederatedRead(read.Read, combiner.Name, read.BuildSide)));
        }

        return [.. found
            .GroupBy(read => read.Component, StringComparer.OrdinalIgnoreCase)
            .Select(group => new FederatedRead(group.Key, group.First().Combiner, group.Any(read => read.BuildSide)))];
    }

    // The store a direct input of a combiner reads; empty when it is not a top-level read whose store can be judged.
    private static async Task<string> StoreOf(PushdownScope scope, string feederName, IReadOnlyList<PushedSubGraph> pushed, CancellationToken cancellationToken)
    {
        if (pushed.FirstOrDefault(sub => string.Equals(sub.Top, feederName, StringComparison.OrdinalIgnoreCase)) is { } sub)
            return await PushdownStores.Key(scope, feederName, sub.Target, cancellationToken).ConfigureAwait(false);

        return scope.Component(feederName) is { ParentComponent: null } feeder
            ? await PushdownParts.ByName(feeder.ComponentType).ReadStore(scope, feeder, cancellationToken).ConfigureAwait(false)
            : string.Empty;
    }
}
