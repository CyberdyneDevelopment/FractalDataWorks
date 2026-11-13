using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Data;

/// <summary>
/// Finds a cycle through the data sets a data set reads, and how deep the nesting goes (FDW-5290): the one function the check before save,
/// and the story that nests data sets at read (FDW-5299), both call.
/// </summary>
/// <remarks>
/// <para>
/// It knows no kind of data set: the caller says what each data set reads, so a Graph data set's components, a Variation's root and a Compound's
/// sources are all the same edge. It reads in levels, one batched call per level for the names not read yet, never one call per name per
/// visit, then walks what it read in memory. It ends on any input: a name is read once, and the walk keeps a set of the names it is on or done with.
/// </para>
/// <para>
/// It holds no limit. How deep nesting may go is a setting of the story that nests data sets; this returns the depth it found, and that story
/// compares it with the setting. Names compare without regard to case.
/// </para>
/// </remarks>
public static class GraphDataSetCycles
{
    /// <summary>Walks the data sets a start data set reads, directly and through each other.</summary>
    /// <param name="start">The name of the data set being checked.</param>
    /// <param name="startReads">The names the start reads as it is now stated (a draft, not the stored one): it replaces what <paramref name="readSources"/> would say of the start wherever the walk meets it.</param>
    /// <param name="readSources">Answers, for a batch of data set names, the names each reads. A name left out of the answer is a data set that does not exist or reads nothing.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The cycle or the depth found, or the failure of a read.</returns>
    public static async Task<IGenericResult<GraphDataSetCycleResult>> Find(
        string start,
        IReadOnlyList<string> startReads,
        Func<IReadOnlyCollection<string>, CancellationToken, Task<IGenericResult<IReadOnlyDictionary<string, IReadOnlyList<string>>>>> readSources,
        CancellationToken cancellationToken = default)
    {
        var edges = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [start] = [.. startReads.Distinct(StringComparer.OrdinalIgnoreCase)],
        };
        var frontier = Unseen(edges[start], edges);
        while (frontier.Count > 0)
        {
            var read = await readSources(frontier, cancellationToken).ConfigureAwait(false);
            if (read.IsFailure || read.Value is null)
            {
                return read.ToNewResult<GraphDataSetCycleResult>();
            }

            foreach (var name in frontier)
            {
                edges[name] = read.Value.TryGetValue(name, out var reads) ? [.. reads.Distinct(StringComparer.OrdinalIgnoreCase)] : [];
            }

            frontier = [.. frontier.SelectMany(name => Unseen(edges[name], edges)).Distinct(StringComparer.OrdinalIgnoreCase)];
        }

        return GenericResult<GraphDataSetCycleResult>.Success(Walk(start, edges));
    }

    private static List<string> Unseen(IReadOnlyList<string> names, Dictionary<string, IReadOnlyList<string>> edges) =>
        [.. names.Where(name => !edges.ContainsKey(name))];

    // Depth first over what was read, with an explicit stack so a deep chain cannot overflow the call stack. A name is either on the stack or done.
    private static GraphDataSetCycleResult Walk(string start, Dictionary<string, IReadOnlyList<string>> edges)
    {
        var onStack = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { start };
        var depths = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var path = new List<string> { start };
        var next = new List<int> { 0 };
        while (path.Count > 0)
        {
            var top = path[^1];
            var position = next[^1];
            if (position < edges[top].Count)
            {
                next[^1] = position + 1;
                var name = edges[top][position];
                if (onStack.Contains(name))
                {
                    return new GraphDataSetCycleResult([.. path.Skip(path.FindIndex(step => string.Equals(step, name, StringComparison.OrdinalIgnoreCase))), name], 0);
                }

                if (!depths.ContainsKey(name))
                {
                    onStack.Add(name);
                    path.Add(name);
                    next.Add(0);
                }

                continue;
            }

            // Every name `top` reads is done by now: a name on the stack would have ended the walk as a cycle.
            depths[top] = edges[top].Count == 0 ? 0 : edges[top].Max(name => depths[name]) + 1;
            onStack.Remove(top);
            path.RemoveAt(path.Count - 1);
            next.RemoveAt(next.Count - 1);
        }

        return new GraphDataSetCycleResult([], depths[start]);
    }
}
