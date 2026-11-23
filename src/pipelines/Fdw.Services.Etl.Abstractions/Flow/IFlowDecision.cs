using System.Collections.Generic;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>A step that decides (FDW-6134): when it runs it records, for each of its outputs, whether the output fired (<see cref="FlowFired"/>).</summary>
public interface IFlowDecision : IFlowStep
{
    /// <summary>Gets the outputs the decision fires from.</summary>
    IReadOnlyList<string> Outputs { get; }
}
