using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>
/// Builds the engine's step for one kind of stored step (FDW-1403).
/// </summary>
public interface IFlowStepBuilder : ITypeOption<int, IFlowStepBuilder>
{
    /// <summary>Builds the engine's step.</summary>
    /// <param name="component">The stored action or loop, already checked against its kind.</param>
    /// <param name="body">The built steps of a loop's body, in order; empty for a kind that holds no steps.</param>
    /// <param name="context">What the run builds with.</param>
    /// <returns>The step, or the failure that says what the step's settings get wrong.</returns>
    IGenericResult<IFlowStep> Build(PipelineComponentConfiguration component, IReadOnlyList<IFlowStep> body, FlowBuildContext context);
}
