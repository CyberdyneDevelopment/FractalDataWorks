using System.Collections.Generic;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// A component that names error outputs: the output nodes that carry the rows it rejected, apart from the rows it kept.
/// </summary>
/// <remarks>
/// Why the component says so: a link out of one of these nodes carries rejected rows, and the run counts them as rejected. Without it
/// a count of rejected rows would have to guess from node names.
/// </remarks>
public interface IFlowErrorOutputs
{
    /// <summary>Gets the names of the component's error output nodes.</summary>
    IReadOnlyList<string> ErrorOutputs { get; }
}
