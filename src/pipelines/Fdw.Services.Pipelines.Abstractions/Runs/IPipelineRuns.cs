using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Pipelines.Abstractions.Runs;

/// <summary>
/// The pipeline layer's run contract (FDW-6131): every trigger starts, awaits, stops and follows a pipeline run through it, whatever the pipeline's implementation.
/// </summary>
/// <remarks>
/// <see cref="Start"/> reads the pipeline, takes its pipeline type from the implementation its domain row names (a <c>PipelineTypes</c> option, FDW-6244),
/// takes that type's run mode (<see cref="PipelineRunModes"/>) and hands the run to the runner this host registered for the mode (<see cref="IPipelineRunner"/>).
/// An implementation no pipeline type names, and a mode no runner here carries out, are refused.
/// </remarks>
public interface IPipelineRuns
{
    /// <summary>Starts a pipeline run.</summary>
    /// <param name="start">What the run is, who it acts as and what it is given.</param>
    /// <param name="cancellationToken">Cancels the start.</param>
    /// <returns>The run's execution identifier, or the refusal naming what was wrong.</returns>
    Task<IGenericResult<Guid>> Start(PipelineStart start, CancellationToken cancellationToken = default);

    /// <summary>Waits for a run started here with <see cref="PipelineStart.AwaitCompletion"/> to end.</summary>
    /// <param name="executionId">The identifier <see cref="Start"/> returned.</param>
    /// <param name="cancellationToken">Cancels the wait.</param>
    /// <returns>Whether the run succeeded, or why it cannot be awaited.</returns>
    Task<IGenericResult<bool>> Await(Guid executionId, CancellationToken cancellationToken = default);

    /// <summary>Asks a run to stop.</summary>
    /// <param name="executionId">The run's execution.</param>
    /// <param name="reason">Why it is stopped, written to the log.</param>
    /// <returns>Success when the stop reached the run, or why it did not.</returns>
    IGenericResult Stop(Guid executionId, string reason);

    /// <summary>Says where a run is followed live and what state it is in.</summary>
    /// <param name="executionId">The run's execution.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The live group and the run's state, or why the run cannot be read.</returns>
    Task<IGenericResult<PipelineRunFollow>> Follow(Guid executionId, CancellationToken cancellationToken = default);
}
