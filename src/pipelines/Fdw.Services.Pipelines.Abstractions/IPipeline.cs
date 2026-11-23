using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Pipelines.Abstractions;

/// <summary>
/// General runtime base for any pipeline, whatever its implementation (a <c>PipelineTypes</c> option, FDW-6244). Carries the
/// identity/lifecycle surface common to every pipeline; the typed run lives on <see cref="IPipeline{TIn, TOut}"/> (e.g.
/// <c>IEtlPipeline</c>).
/// </summary>
public interface IPipeline : IDisposable, IGenericService
{
    /// <summary>
    /// Gets the name of this pipeline.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the pipeline type name (the engine discriminator, "Graph").
    /// </summary>
    string PipelineType { get; }

    /// <summary>
    /// Gets whether this pipeline is currently executing.
    /// </summary>
    bool IsExecuting { get; }

    /// <summary>
    /// Validates the pipeline configuration before execution.
    /// </summary>
    /// <returns>Validation result.</returns>
    IGenericResult Validate();
}

/// <summary>
/// A pipeline that takes a <typeparamref name="TIn"/> and answers with a <typeparamref name="TOut"/> (FDW-6244): the typed run of a pipeline type's
/// implementation. <c>IEtlPipeline</c> is its ETL closing, which takes the execution options and answers with the execution result.
/// </summary>
/// <typeparam name="TIn">What a run of the pipeline takes.</typeparam>
/// <typeparam name="TOut">What a run of the pipeline answers with.</typeparam>
public interface IPipeline<TIn, TOut> : IPipeline
{
    /// <summary>Runs the pipeline.</summary>
    /// <param name="options">What the run takes (named <c>options</c> so the ETL implementations, whose parameter carries that name, match it).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>What the run answers with, or why it failed.</returns>
    Task<IGenericResult<TOut>> Execute(TIn options, CancellationToken cancellationToken = default);
}
