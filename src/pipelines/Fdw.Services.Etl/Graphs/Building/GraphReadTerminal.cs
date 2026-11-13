using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>
/// Where a graph that is read and not loaded ends (FDW-5291): the component output that carries the rows, and what takes each batch of them.
/// </summary>
/// <remarks>
/// A graph data set has no destination, so a read adds one component of its own after the output named here and hands each batch to
/// <see cref="Write"/>. The stored graph is not changed and holds no such component.
/// </remarks>
/// <param name="Component">The component whose output carries the rows.</param>
/// <param name="Output">The output of that component.</param>
/// <param name="Write">Takes one batch; a failure it answers stops the run.</param>
public sealed record GraphReadTerminal(string Component, string Output, Func<FlowBatch, CancellationToken, Task<IGenericResult>> Write)
{
    /// <summary>Gets the end of the graph this terminal takes the rows of.</summary>
    public GraphEnd End => new(Component, Output);
}
