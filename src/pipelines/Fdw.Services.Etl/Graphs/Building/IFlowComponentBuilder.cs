using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>
/// Turns one stored component of a graph into the flow component that runs it (FDW-1396). One builder per
/// <c>ComponentTypes</c> option, found by the option's name.
/// </summary>
public interface IFlowComponentBuilder : ITypeOption<int, IFlowComponentBuilder>
{
    /// <summary>Builds the flow component of a stored component whose settings have been checked against its kind.</summary>
    /// <param name="component">The stored component.</param>
    /// <param name="context">What the run builds with.</param>
    /// <returns>The flow component, or the failure that says what the component's settings get wrong.</returns>
    IGenericResult<IFlowComponent> Build(PipelineComponentConfiguration component, FlowBuildContext context);
}
