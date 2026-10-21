using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.DataSets.Abstractions;

namespace Fdw.Operations.Endpoints;

/// <summary>
/// Decides which data sets belong in the dataflow graph: the ones the existing pipelines reach.
/// </summary>
/// <remarks>
/// A pipeline reaches the data sets it names as source or destination, and every data set joined to
/// one of those by derivation (a source that names another data set), in either direction. A data
/// set no pipeline reaches has no lineage, so it is not drawn.
/// </remarks>
internal static class DataflowReachability
{
    /// <summary>The data sets the pipelines reach, in the order they were given.</summary>
    public static IReadOnlyList<IDataSetImplementationConfiguration> Owners(
        IReadOnlyList<PipelineLineageRecord> pipelines,
        IReadOnlyList<IDataSetImplementationConfiguration> owners)
    {
        var named = NamedByPipelines(pipelines);
        var reached = new HashSet<Guid>();
        var frontier = new Queue<IDataSetImplementationConfiguration>();

        foreach (var seed in owners.Where(o => named.Contains(o.Name)))
            Reach(seed, reached, frontier);

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            foreach (var neighbour in owners.Where(o => !reached.Contains(o.Id) && AreNeighbours(current, o)).ToList())
                Reach(neighbour, reached, frontier);
        }

        return owners.Where(o => reached.Contains(o.Id)).ToList();
    }

    private static HashSet<string> NamedByPipelines(IReadOnlyList<PipelineLineageRecord> pipelines)
        => pipelines
            .SelectMany(p => p.DataSetsRead.Concat(p.DataSetsWritten))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static void Reach(
        IDataSetImplementationConfiguration owner,
        HashSet<Guid> reached,
        Queue<IDataSetImplementationConfiguration> frontier)
    {
        if (reached.Add(owner.Id))
            frontier.Enqueue(owner);
    }

    private static bool AreNeighbours(IDataSetImplementationConfiguration a, IDataSetImplementationConfiguration b)
        => Derives(a, b) || Derives(b, a);

    private static bool Derives(IDataSetImplementationConfiguration derived, IDataSetImplementationConfiguration origin)
        => IsNamed(derived.SourceDataSetName, origin.Name)
           || derived.Sources.Any(s => s.SourceDataSetId == origin.Id || IsNamed(s.SourceDataSetName, origin.Name));

    private static bool IsNamed(string? candidate, string name)
        => !string.IsNullOrEmpty(candidate) && string.Equals(candidate, name, StringComparison.OrdinalIgnoreCase);
}
