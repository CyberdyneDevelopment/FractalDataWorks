using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// The component output a graph that is read and not loaded ends in (FDW-5291): the output that carries the rows of a Graph data set's fields out.
/// </summary>
/// <remarks>
/// A graph data set holds no destination, so its read adds one component of its own, <see cref="ReaderName"/>, that takes the rows of this output.
/// That is the one rule of an end: its output is consumed, so it is neither unlinked nor lost. The read builds the line to that component with
/// <see cref="ToReader"/>, and the shape check judges the same line over a stand-in of it, so what the check calls sound is what the read builds.
/// </remarks>
/// <param name="Component">The component whose output carries the rows.</param>
/// <param name="Output">The output of that component.</param>
public sealed record GraphEnd(string Component, string Output)
{
    /// <summary>The name of the component a read adds to take the rows of the end. A stored component that already has it is refused as a repeated name.</summary>
    public const string ReaderName = "$read";

    /// <summary>Makes the line from the end to the read's own component.</summary>
    /// <returns>The line.</returns>
    public FlowLink ToReader() => new(Component, Output, ReaderName, FlowDestination.InputName);
}
