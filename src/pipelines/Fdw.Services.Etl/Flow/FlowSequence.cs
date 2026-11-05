using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// Runs steps one after another, each only when the one before has finished (FDW-1403): the barrier between stages.
/// </summary>
internal static class FlowSequence
{
    /// <summary>Runs the steps in order and stops at the first failure.</summary>
    /// <param name="steps">The steps.</param>
    /// <param name="context">What they run against.</param>
    /// <param name="cancellationToken">Cancels the run.</param>
    /// <returns>Success, or the first step's failure.</returns>
    public static async Task<IGenericResult> Run(IReadOnlyList<IFlowStep> steps, FlowStepContext context, CancellationToken cancellationToken)
    {
        foreach (var step in steps)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await RunReported(step, context, cancellationToken).ConfigureAwait(false);
            if (result.IsFailure)
            {
                return result;
            }
        }

        return GenericResult.Success();
    }

    /// <summary>Runs the body of a loop once and records the round, "Round 1", "Round 2", under the loop's record (FDW-2060).</summary>
    /// <param name="body">The steps of the loop's body.</param>
    /// <param name="context">What the body runs against; its report parent is the loop's record.</param>
    /// <param name="round">The round's number, from 1.</param>
    /// <param name="cancellationToken">Cancels the run.</param>
    /// <returns>Success, or the first step's failure.</returns>
    public static async Task<IGenericResult> RunRound(IReadOnlyList<IFlowStep> body, FlowStepContext context, int round, CancellationToken cancellationToken)
    {
        if (context.Reporter is not { } reporter || await reporter.StepStarting($"Round {round}", context.ReportParent, cancellationToken).ConfigureAwait(false) is not { } record)
        {
            return await Run(body, context, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            var result = await Run(body, context.WithReportParent(record), cancellationToken).ConfigureAwait(false);
            await Finished(reporter, record, result, HostStopped(context), cancellationToken).ConfigureAwait(false);
            return result;
        }
        catch (OperationCanceledException)
        {
            if (!HostStopped(context))
            {
                await reporter.StepCancelled(record, CancellationToken.None).ConfigureAwait(false);
            }

            throw;
        }
    }

    // A tracked run records each step as a stage under the step it runs in. A step whose record could not be written still runs: the
    // record observes the run, it is not part of it.
    internal static async Task<IGenericResult> RunReported(IFlowStep step, FlowStepContext context, CancellationToken cancellationToken)
    {
        // Why a level is not a record of its own (FDW-2060): it records its stages, one for each stage number, under the step it runs in.
        if (step is FlowStageStep)
        {
            return await step.Run(context, cancellationToken).ConfigureAwait(false);
        }

        if (context.Reporter is not { } reporter || await reporter.StepStarting(step.Name, context.ReportParent, cancellationToken).ConfigureAwait(false) is not { } stage)
        {
            return await step.Run(context, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            var result = await step.Run(context.WithReportParent(stage), cancellationToken).ConfigureAwait(false);
            await Finished(reporter, stage, result, HostStopped(context), cancellationToken).ConfigureAwait(false);
            return result;
        }
        catch (OperationCanceledException)
        {
            if (!HostStopped(context))
            {
                await reporter.StepCancelled(stage, CancellationToken.None).ConfigureAwait(false);
            }

            throw;
        }
    }

    // Why a failure under a cancel is reported as a cancel (FDW-3784): a step that answers its caller's cancel with a failed result, and not by throwing, was
    // cancelled and did not fail; its record says so, and says it without the cancelled token, which would refuse the write.
    // A host stop leaves the record open (FDW-3880): the host closes everything still open under the run Interrupted.
    private static Task Finished(IFlowReporter reporter, Guid record, IGenericResult result, bool hostStopped, CancellationToken cancellationToken) =>
        cancellationToken.IsCancellationRequested && result.IsFailure
            ? hostStopped ? Task.CompletedTask : reporter.StepCancelled(record, CancellationToken.None)
            : reporter.StepFinished(record, result, cancellationToken);

    // Whether the run's token ended because the host is stopping and no person cancelled the run (FDW-3880).
    private static bool HostStopped(FlowStepContext context) => context.PauseGate?.StoppedWithHost == true;
}
