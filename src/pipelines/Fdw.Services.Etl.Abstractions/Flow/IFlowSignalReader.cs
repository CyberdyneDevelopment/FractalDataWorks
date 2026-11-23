using System.Collections.Generic;
using Fdw.Results;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// A decision that reads the signal lines that feed it as values and never as the reason to run (FDW-6134): a gate. A step that is not one is skipped when none of the lines that
/// feed it fired.
/// </summary>
public interface IFlowSignalReader : IFlowDecision
{
    /// <summary>Gives the step the lines that feed it, once, when its graph is built.</summary>
    /// <param name="signals">The lines, in the order they are stored.</param>
    /// <returns>Success, or the refusal of a number of lines the step does not take.</returns>
    IGenericResult ReadSignals(IReadOnlyList<FlowSignal> signals);
}
