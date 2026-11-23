using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;

namespace Fdw.Services.Etl.Graphs.Planning;

/// <summary>
/// What a store runs of one component type when a graph is pushed into it (FDW-6969). One option per <c>ComponentTypes</c> option the planner pushes,
/// found by the option's name; a component type with no option here always runs in memory.
/// </summary>
public interface IPushdownPart : ITypeOption<int, IPushdownPart>
{
    /// <summary>Gets whether the component combines inputs (a join, a lookup, a union): such a component is where a pushed sub-graph may end.</summary>
    bool Combines { get; }

    /// <summary>Gets the output the component sends its rows out by.</summary>
    string OutputName { get; }

    /// <summary>Gets the input the component holds whole while it streams its other input (a join's right, a lookup's reference), or empty when none (FDW-6970).</summary>
    string BuildSideInput { get; }

    /// <summary>Judges which store a component of this type reads, for a combiner across stores (FDW-6970).</summary>
    /// <param name="scope">The graph.</param>
    /// <param name="component">The component.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A key two reads of one store share; empty when the component is not a read, or its store cannot be judged.</returns>
    Task<string> ReadStore(PushdownScope scope, PipelineComponentConfiguration component, CancellationToken cancellationToken = default);

    /// <summary>Resolves what the store runs for the component and what feeds it.</summary>
    /// <param name="scope">The graph.</param>
    /// <param name="component">The component.</param>
    /// <param name="top">Whether it would be the top of a pushed sub-graph, whose outputs leave it.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The side it reads once pushed, or why it runs in memory.</returns>
    Task<PushdownAttempt> Resolve(PushdownScope scope, PipelineComponentConfiguration component, bool top, CancellationToken cancellationToken = default);
}
