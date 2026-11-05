using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// Judges the completion lines of a level before anything runs (FDW-3640), so a run can never wait for itself.
/// </summary>
/// <remarks>
/// <para>
/// A completion line makes one component start only when another has finished. It is refused, naming the components, when: it starts or ends at a component that
/// does not exist, or is given twice; it joins a component to itself; it joins two components that share row lines, directly or through others, because
/// components joined by rows run together as one data flow and one would wait for the other while the other waits to hand it rows; it runs from a later stage to
/// an earlier one, which runs first; or the completion lines, with the stage order, make a cycle among the groups of components that run together.
/// </para>
/// <para>
/// It works over names so the save, which leaves actions out of the engine's flow check, and the run, which has them as components, judge by the same rule.
/// </para>
/// </remarks>
public static class FlowCompletionValidator
{
    /// <summary>Lists every rule the completion lines break.</summary>
    /// <param name="components">The names of every component of the level, actions and loops included.</param>
    /// <param name="rowLinks">The lines that carry rows.</param>
    /// <param name="completions">The completion lines.</param>
    /// <param name="rankOf">Says what rank a component runs at: 0 for none, its stage number otherwise.</param>
    /// <returns>The problems; each carries the position of its line in <paramref name="completions"/> and the node kind <c>completion</c>; empty when the lines are sound.</returns>
    public static IReadOnlyList<FlowGraphProblem> Problems(
        IReadOnlyList<string> components, IReadOnlyList<FlowLink> rowLinks, IReadOnlyList<FlowCompletion> completions, Func<string, int> rankOf)
    {
        ArgumentNullException.ThrowIfNull(components);
        ArgumentNullException.ThrowIfNull(rowLinks);
        ArgumentNullException.ThrowIfNull(completions);
        ArgumentNullException.ThrowIfNull(rankOf);
        if (completions.Count == 0)
        {
            return [];
        }

        var groups = RowGroups(components, rowLinks);
        var problems = new List<FlowGraphProblem>();
        var sound = new List<FlowCompletion>();
        var seen = new HashSet<FlowCompletion>(new LineComparer());
        for (var index = 0; index < completions.Count; index++)
        {
            var line = completions[index];
            var problem = LineProblem(line, index, groups, rankOf);
            if (problem is null && !seen.Add(line))
            {
                problem = Problem($"the completion line {Describe(line)} is given twice", line, index);
            }

            if (problem is null)
            {
                sound.Add(line);
            }
            else
            {
                problems.Add(problem);
            }
        }

        problems.AddRange(CycleProblems(groups, sound, rankOf));
        return problems;
    }

    // Components joined by row lines, in either direction, run together: each gets the number of the group it is in.
    private static Dictionary<string, int> RowGroups(IReadOnlyList<string> components, IReadOnlyList<FlowLink> rowLinks)
    {
        var parent = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in components.Where(name => !string.IsNullOrWhiteSpace(name)))
        {
            parent.TryAdd(name, name);
        }

        string Root(string name)
        {
            while (!string.Equals(parent[name], name, StringComparison.OrdinalIgnoreCase))
            {
                parent[name] = parent[parent[name]];
                name = parent[name];
            }

            return name;
        }

        foreach (var link in rowLinks.Where(link => parent.ContainsKey(link.FromComponent) && parent.ContainsKey(link.ToComponent)))
        {
            parent[Root(link.FromComponent)] = Root(link.ToComponent);
        }

        var numbers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        return parent.Keys.ToDictionary(name => name, name => numbers.TryGetValue(Root(name), out var number) ? number : numbers[Root(name)] = numbers.Count, StringComparer.OrdinalIgnoreCase);
    }

    private static FlowGraphProblem? LineProblem(FlowCompletion line, int index, Dictionary<string, int> groups, Func<string, int> rankOf)
    {
        if (!groups.TryGetValue(line.FromComponent, out var from))
        {
            return Problem($"the completion line {Describe(line)} starts at a component that does not exist", line, index);
        }

        if (!groups.TryGetValue(line.ToComponent, out var to))
        {
            return Problem($"the completion line {Describe(line)} ends at a component that does not exist", line, index);
        }

        if (string.Equals(line.FromComponent, line.ToComponent, StringComparison.OrdinalIgnoreCase))
        {
            return Problem($"the completion line {Describe(line)} makes '{line.FromComponent}' wait for itself", line, index);
        }

        if (from == to)
        {
            return Problem(
                $"the completion line {Describe(line)} joins '{line.FromComponent}' and '{line.ToComponent}', which pass rows to one another through lines and so run together: "
                + $"'{line.ToComponent}' would wait for '{line.FromComponent}' to finish while '{line.FromComponent}' waits for '{line.ToComponent}' to take its rows",
                line,
                index);
        }

        var fromRank = rankOf(line.FromComponent);
        var toRank = rankOf(line.ToComponent);
        return fromRank > toRank
            ? Problem(
                $"the completion line {Describe(line)} runs from stage {RankText(fromRank)} to stage {RankText(toRank)}, which runs first; a completion line may only go to the same or a later stage",
                line,
                index)
            : null;
    }

    // Groups are the nodes; an edge says one group must have finished before another can finish: a completion line, and a lower stage before a higher one. A cycle
    // that the stage order alone makes is not this validator's to report, so only what the completion lines add is.
    private static IEnumerable<FlowGraphProblem> CycleProblems(Dictionary<string, int> groups, List<FlowCompletion> sound, Func<string, int> rankOf)
    {
        if (sound.Count == 0)
        {
            yield break;
        }

        var ids = groups.Values.Distinct().ToList();
        var ranks = ids.ToDictionary(
            id => id, id => groups.Where(group => group.Value == id).Select(group => rankOf(group.Key)).ToList());
        var stageEdges = ids.SelectMany(from => ids.Where(to => from != to && ranks[from].Min() < ranks[to].Max()).Select(to => (from, to))).ToList();
        var completionEdges = sound.Select(line => (from: groups[line.FromComponent], to: groups[line.ToComponent])).Where(edge => edge.from != edge.to).ToList();
        var added = Stuck(ids, [.. stageEdges, .. completionEdges]).Except(Stuck(ids, stageEdges)).ToHashSet();
        if (added.Count == 0)
        {
            yield break;
        }

        var names = groups.Where(group => added.Contains(group.Value)).Select(group => group.Key).OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToList();
        yield return new FlowGraphProblem(
            $"the completion lines, with the order of the stages, form a cycle through {string.Join(", ", names.Select(name => $"'{name}'"))}", names[0]);
    }

    // Kahn's algorithm: whatever cannot be taken off the front is in, or behind, a cycle.
    private static List<int> Stuck(List<int> ids, List<(int from, int to)> edges)
    {
        var waiting = ids.ToDictionary(id => id, id => edges.Count(edge => edge.to == id));
        var ready = new Queue<int>(waiting.Where(entry => entry.Value == 0).Select(entry => entry.Key));
        var removed = new HashSet<int>();
        while (ready.Count > 0)
        {
            var current = ready.Dequeue();
            removed.Add(current);
            foreach (var edge in edges.Where(edge => edge.from == current))
            {
                if (--waiting[edge.to] == 0)
                {
                    ready.Enqueue(edge.to);
                }
            }
        }

        return [.. ids.Where(id => !removed.Contains(id))];
    }

    private static FlowGraphProblem Problem(string reason, FlowCompletion line, int index) =>
        new(reason, line.FromComponent, CompletionNodes.Done, "completion", index);

    private static string Describe(FlowCompletion line) => $"'{line.FromComponent}.{CompletionNodes.Done}' -> '{line.ToComponent}'";

    private static string RankText(int rank) => rank == 0 ? "(none)" : rank.ToString(CultureInfo.InvariantCulture);

    // Two lines are the same when they join the same components, whatever the case of their names.
    private sealed class LineComparer : IEqualityComparer<FlowCompletion>
    {
        public bool Equals(FlowCompletion? x, FlowCompletion? y) =>
            x is not null && y is not null
            && string.Equals(x.FromComponent, y.FromComponent, StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.ToComponent, y.ToComponent, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode(FlowCompletion obj) =>
            HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(obj.FromComponent), StringComparer.OrdinalIgnoreCase.GetHashCode(obj.ToComponent));
    }
}
