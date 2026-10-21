using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using System;

namespace Fdw.Services.Etl.Abstractions.Execution;

/// <summary>
/// Starts a pipeline run under another execution: the one way a run begins when no person or schedule triggers it (FDW-1381).
/// </summary>
/// <remarks>
/// <para>
/// An orchestration node starts its member pipelines through it, and a StartPipeline stage starts its target through it, so a
/// started run is always the same thing: an execution item that is the child of the run that started it and shares its
/// correlation, a metrics row that holds its start and end, and a queued request that carries its tenant, roles and parameters.
/// </para>
/// <para>
/// Why not the trigger endpoint's own path: that path judges what a person may do (who the caller is, which roles they hold).
/// A run started by a run acts with the roles of the run that started it, so it can never hold more.
/// </para>
/// </remarks>
public interface IPipelineRunStarter
{
    /// <summary>Starts a pipeline run under the execution the start names.</summary>
    /// <param name="start">What the run is, who it acts as and what it is given.</param>
    /// <param name="cancellationToken">Cancels the start.</param>
    /// <returns>The new run's execution identifier, or the refusal naming what was wrong.</returns>
    Task<IGenericResult<Guid>> Start(PipelineRunStart start, CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirms that a pipeline named by a StartPipeline stage can be started with the parameter names the stage binds: it exists
    /// in the caller's view, every name is one it declares and every parameter it requires is named.
    /// </summary>
    /// <param name="pipelineName">The name of the pipeline to start.</param>
    /// <param name="parameterNames">The names of the parameters the stage binds.</param>
    /// <param name="cancellationToken">Cancels the check.</param>
    /// <returns>Success, or the refusal naming what was wrong.</returns>
    Task<IGenericResult> Check(string pipelineName, IReadOnlyList<string> parameterNames, CancellationToken cancellationToken = default);

    /// <summary>Waits for a run started with <see cref="PipelineRunStart.AwaitCompletion"/> to end.</summary>
    /// <param name="executionId">The identifier <see cref="Start"/> returned.</param>
    /// <param name="cancellationToken">Cancels the wait.</param>
    /// <returns>Whether the run succeeded.</returns>
    Task<bool> Await(Guid executionId, CancellationToken cancellationToken = default);
}
