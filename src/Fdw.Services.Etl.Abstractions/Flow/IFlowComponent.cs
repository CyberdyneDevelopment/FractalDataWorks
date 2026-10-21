using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// One component of a flow: it declares its input and output nodes and, when the run starts, reads rows from the inputs and
/// writes rows to the outputs until its inputs are finished (FDW-1386).
/// </summary>
/// <remarks>
/// A component never knows what is linked to its nodes. It reads a named input and writes a named output; the graph decides
/// where rows come from and go to. A run executes every component as its own task. What a component is to the flow follows from
/// its nodes: one with no inputs is a source, one with no outputs is a destination, one with both transforms rows.
/// </remarks>
public interface IFlowComponent
{
    /// <summary>Gets the component's name, unique within the graph.</summary>
    string Name { get; }

    /// <summary>Gets the names of the component's input nodes.</summary>
    IReadOnlyList<string> Inputs { get; }

    /// <summary>Gets the names of the component's output nodes.</summary>
    IReadOnlyList<string> Outputs { get; }

    /// <summary>Runs the component to the end of its inputs.</summary>
    /// <param name="ports">The input and output nodes of this component in this run.</param>
    /// <param name="cancellationToken">Cancelled when the run is cancelled or another component fails.</param>
    /// <returns>Success, or the failure that stops the run.</returns>
    Task<IGenericResult> Run(IFlowPorts ports, CancellationToken cancellationToken = default);
}
