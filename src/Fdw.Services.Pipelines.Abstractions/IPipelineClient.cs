using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// Defines the contract for interacting with the pipeline configuration API.
/// </summary>
public interface IPipelineClient
{
    /// <summary>
    /// Lists all configured pipelines.
    /// </summary>
    Task<IGenericResult<IReadOnlyList<PipelineSummaryResponse>>> List(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a pipeline by name.
    /// </summary>
    Task<IGenericResult<PipelineDetailResponse>> Get(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a pipeline by its logical id, as the caller sees it (FDW-1088).
    /// </summary>
    Task<IGenericResult<PipelineDetailResponse>> Get(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new pipeline.
    /// </summary>
    Task<IGenericResult<PipelineDetailResponse>> CreatePipeline(CreatePipelineClientRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks a pipeline draft exactly as creating it would, without saving anything, and returns every problem found.
    /// </summary>
    Task<IGenericResult<ValidatePipelineResponse>> ValidatePipeline(CreatePipelineClientRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks an edit of a saved pipeline exactly as updating it would, without saving anything, and returns every problem
    /// found (FDW-1105). The pipeline must exist.
    /// </summary>
    Task<IGenericResult<ValidatePipelineResponse>> ValidatePipeline(string name, UpdatePipelineClientRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing pipeline by name.
    /// </summary>
    Task<IGenericResult<PipelineDetailResponse>> UpdatePipeline(string name, UpdatePipelineClientRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all registered ETL pipeline engine types (e.g., "BatchCopy", "Streaming"), sourced from
    /// the server-side <c>EtlPipelineTypes</c> ServiceTypeCollection — never hardcoded client-side.
    /// </summary>
    Task<IGenericResult<IReadOnlyList<PipelineTypeSummary>>> GetPipelineTypes(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a pipeline by name.
    /// </summary>
    Task<IGenericResult> DeletePipeline(string name, CancellationToken cancellationToken = default);
}
