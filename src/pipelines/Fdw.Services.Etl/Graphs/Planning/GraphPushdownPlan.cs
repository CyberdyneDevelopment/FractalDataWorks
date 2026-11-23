using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.Services.Etl.Graphs.Planning;

/// <summary>
/// The planner's answer for a graph (FDW-6969): the sub-graphs that run as one query in their store, and each join, union or lookup that runs in
/// memory with why. A graph with nothing pushed runs as it always has.
/// </summary>
/// <param name="Pushed">The sub-graphs pushed into their stores.</param>
/// <param name="InMemory">The joins, unions and lookups left in memory, each with why.</param>
/// <param name="FederatedReads">The reads that feed a join, lookup or union across data stores, which run under the federation strategy (FDW-6970).</param>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed record GraphPushdownPlan(
    IReadOnlyList<PushedSubGraph> Pushed, IReadOnlyList<PushdownLeftInMemory> InMemory, IReadOnlyList<FederatedRead> FederatedReads)
{
    /// <summary>Gets the plan of a graph that runs in memory whole.</summary>
    public static GraphPushdownPlan None { get; } = new([], [], []);

    /// <summary>Finds the federated read a component is.</summary>
    /// <param name="component">The component's name.</param>
    /// <returns>The read, or null when the component is not one.</returns>
    public FederatedRead? FederatedRead(string component) =>
        FederatedReads.FirstOrDefault(read => string.Equals(read.Component, component, StringComparison.OrdinalIgnoreCase));

    /// <summary>Finds the sub-graph a component is the top of.</summary>
    /// <param name="component">The component's name.</param>
    /// <returns>The sub-graph, or null when the component tops none.</returns>
    public PushedSubGraph? ToppedBy(string component) =>
        Pushed.FirstOrDefault(pushed => string.Equals(pushed.Top, component, StringComparison.OrdinalIgnoreCase));

    /// <summary>Gets whether a component is removed from the graph, read by a pushed query in its place.</summary>
    /// <param name="component">The component's name.</param>
    /// <returns>True when a pushed sub-graph stands in for it below its top.</returns>
    public bool Removes(string component) => Pushed.Any(pushed => pushed.Removes(component));
}
