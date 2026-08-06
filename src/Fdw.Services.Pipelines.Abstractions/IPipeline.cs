using System;
using Fdw.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Pipelines.Abstractions;

/// <summary>
/// General runtime base for any pipeline KIND (ETL today; other kinds in future). Carries the
/// identity/lifecycle surface common to every pipeline; kind-specific execution lives on the
/// derived interface (e.g. <c>IEtlPipeline</c>).
/// </summary>
public interface IPipeline : IDisposable, IGenericService
{
    /// <summary>
    /// Gets the unique identifier for this pipeline instance.
    /// </summary>
    // Why: shadows IGenericService.Id (string) with the pipeline's durable Guid identity.
    new Guid Id { get; }

    /// <summary>
    /// Gets the name of this pipeline.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the pipeline type name (the engine discriminator, e.g. "BatchCopy", "Streaming").
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
