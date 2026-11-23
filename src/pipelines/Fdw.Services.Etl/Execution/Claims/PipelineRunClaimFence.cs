using System;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Execution.Claims;

/// <summary>
/// The fence of a run that holds a durable claim (FDW-6939): before each step the run asks whether it still holds its claim. Past the lease, or after a renewal found it holds no row, the claim may already be
/// another run's, so the step is refused with the lease code and the run's own step limit is not asked. A run that still holds its claim takes its steps from its own step limit, or freely when it has none.
/// </summary>
/// <param name="inner">The run's own step limit, or null when it has none.</param>
/// <param name="check">Says whether the run still holds its claim (<see cref="PipelineRunClaimHold.Check"/>).</param>
/// <param name="logger">The logger the refusal is written to.</param>
public sealed class PipelineRunClaimFence(IFlowStepLimit? inner, Func<ILogger, IGenericResult> check, ILogger logger) : IFlowStepLimit
{
    /// <inheritdoc />
    public IGenericResult TakeStep(string stepName)
    {
        var holds = check(logger);
        if (holds.IsFailure)
            return holds;

        return inner is null ? GenericResult.Success() : inner.TakeStep(stepName);
    }
}
