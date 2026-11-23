using Fdw.Results;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// The limit on the steps of a run (FDW-6141): a flow takes a step from it before the step runs, and a step the limit does not allow is not started.
/// </summary>
public interface IFlowStepLimit
{
    /// <summary>Takes one step before it runs.</summary>
    /// <param name="stepName">The name of the step about to run, which a refusal names.</param>
    /// <returns>Success when the step may run; the coded failure when the run has no step left.</returns>
    IGenericResult TakeStep(string stepName);
}
