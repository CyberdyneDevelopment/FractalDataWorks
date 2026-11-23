using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Pipelines.Abstractions.Runs;

/// <summary>
/// Carries out the runs of one run mode (FDW-6131). The package that hosts the mode registers its runner; <see cref="IPipelineRuns"/> hands each run to
/// the runner whose <see cref="RunMode"/> the pipeline's type names (<see cref="IPipelineType.RunModeName"/>).
/// </summary>
public interface IPipelineRunner
{
    /// <summary>Gets the run mode this runner carries out.</summary>
    IPipelineRunMode RunMode { get; }

    /// <summary>Starts a run of a pipeline that has been read and whose pipeline type takes this runner's mode.</summary>
    /// <param name="start">What the caller stated about the run.</param>
    /// <param name="pipeline">The pipeline, as a read of the Pipeline domain handed it back.</param>
    /// <param name="cancellationToken">Cancels the start.</param>
    /// <returns>The run's execution identifier, or the refusal naming what was wrong.</returns>
    Task<IGenericResult<Guid>> Start(PipelineStart start, IPipelineImplementationConfiguration pipeline, CancellationToken cancellationToken = default);

    /// <summary>Waits for a run this runner started with <see cref="PipelineStart.AwaitCompletion"/> to end.</summary>
    /// <param name="executionId">The run's execution.</param>
    /// <param name="cancellationToken">Cancels the wait.</param>
    /// <returns>Whether the run succeeded, or why it cannot be awaited.</returns>
    Task<IGenericResult<bool>> Await(Guid executionId, CancellationToken cancellationToken = default);
}
