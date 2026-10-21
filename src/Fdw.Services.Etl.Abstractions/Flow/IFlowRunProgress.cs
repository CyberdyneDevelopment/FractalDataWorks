using System.Collections.Generic;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// Told by the executor as the components of a run finish, so whatever counts the rows of a run can answer while the run is still going (FDW-2041).
/// </summary>
/// <remarks>
/// Why a run is told and does not count at its end: an action or a loop is a component of the same graph as the components whose rows it reports
/// (a Notify of a later stage says how many rows an earlier stage read), so the counts must be there before the graph is done.
/// </remarks>
public interface IFlowRunProgress
{
    /// <summary>Notes that a component has finished: it has run and closed its outputs, however it ended.</summary>
    /// <param name="component">The component that finished.</param>
    /// <param name="links">The rows carried so far by every link of the graph, which for the links of the component that finished are final.</param>
    void ComponentFinished(IFlowComponent component, IReadOnlyList<FlowLinkCount> links);
}
