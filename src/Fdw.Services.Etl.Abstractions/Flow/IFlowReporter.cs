using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// Records how the steps of a run went (FDW-1602): each step as a stage of the run with its start, its end and its outcome, and what
/// each data flow did.
/// </summary>
/// <remarks>
/// Why a reporter and not the steps writing to the tracker: the steps run the same with or without it. A run that nobody tracks passes
/// none, and records nothing. A reporter that cannot write logs that itself and never fails the run: the report observes the run, it is
/// not part of it.
/// </remarks>
public interface IFlowReporter
{
    /// <summary>Records that a step is starting.</summary>
    /// <param name="stepName">The step's name.</param>
    /// <param name="parent">The stage record the step runs under; none for a step of the run itself.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>The identifier of the stage record, or none when it could not be written.</returns>
    Task<Guid?> StepStarting(string stepName, Guid? parent, CancellationToken cancellationToken = default);

    /// <summary>Records how a step ended.</summary>
    /// <param name="stage">The stage record <see cref="StepStarting"/> returned.</param>
    /// <param name="outcome">The step's outcome.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>A task that completes when the outcome is recorded.</returns>
    Task StepFinished(Guid stage, IGenericResult outcome, CancellationToken cancellationToken = default);

    /// <summary>Records that a step was cancelled before it ended.</summary>
    /// <param name="stage">The stage record <see cref="StepStarting"/> returned.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>A task that completes when the outcome is recorded.</returns>
    Task StepCancelled(Guid stage, CancellationToken cancellationToken = default);

    /// <summary>Records what a data flow did: when each component started and ended and how many rows and batches crossed each link.</summary>
    /// <param name="stage">The stage record of the data flow step.</param>
    /// <param name="report">The report the executor returned.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>A task that completes when the events are recorded.</returns>
    Task FlowFinished(Guid stage, FlowRunReport report, CancellationToken cancellationToken = default);
}
