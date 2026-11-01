using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// A component of a flow graph that runs as a step of the flow (FDW-2041): it is given what a step runs against (the variables in scope, the
/// executor) before the graph it is in runs.
/// </summary>
internal interface IFlowStepBound
{
    /// <summary>Gives the component what it runs against in this run of its graph.</summary>
    /// <param name="context">The variables and executor of the step the graph runs in.</param>
    void Bind(FlowStepContext context);
}
