using System.Linq;
using Fdw.Data.DataSets.Abstractions;

namespace Fdw.Data.DataSets;

/// <summary>Whether a Graph data set is bound or still abstract (FDW-5294).</summary>
/// <remarks>
/// A Graph data set is bound when it has a graph: it has a component, or a component feeds a field out (the field's <c>FedByComponent</c>
/// is set). While it is abstract it has fields, perhaps intended sources, and no graph. This is the one test of "bound"; whatever a later
/// slice stores of the graph body extends it here rather than adding a second test. A bound graph that feeds no field is a problem for the
/// check (story 2) to name, not a data set that is "not bound yet".
/// </remarks>
public static class GraphDataSetBinding
{
    /// <summary>Gets whether the data set has a graph.</summary>
    /// <param name="graph">The Graph data set.</param>
    /// <returns>True when it has a component or a field out is fed by a component; false while the data set is abstract.</returns>
    public static bool IsBound(this IGraphDataSetConfiguration graph) =>
        graph.Components.Count > 0 || graph.GraphFields.Any(field => field.FedByComponent is not null);
}
