using System.Linq;
using Fdw.Data.DataSets.Abstractions;

namespace Fdw.Data.DataSets;

/// <summary>Works out the state of a field out of a Graph data set (FDW-5294).</summary>
/// <remarks>
/// The state is read, never stored: the lowest-precedence <see cref="GraphFieldStates"/> option that applies. Defined applies to every field, so
/// a field always has a state.
/// </remarks>
public static class GraphDataSetFieldStates
{
    /// <summary>Gets the state of one field of the data set.</summary>
    /// <param name="graph">The Graph data set the field belongs to.</param>
    /// <param name="field">The field out.</param>
    /// <returns>The state that applies to the field.</returns>
    public static IGraphFieldState StateOf(this IGraphDataSetConfiguration graph, GraphDataSetFieldConfiguration field) =>
        GraphFieldStates.All()
            .OrderBy(state => state.Precedence)
            .First(state => state.Applies(field, graph.Intents.Count > 0));
}
