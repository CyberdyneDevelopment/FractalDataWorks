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
    /// Creates a new pipeline.
    /// </summary>
    Task<IGenericResult<PipelineDetailResponse>> CreatePipeline(CreatePipelineClientRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing pipeline by name.
    /// </summary>
    Task<IGenericResult<PipelineDetailResponse>> UpdatePipeline(string name, UpdatePipelineClientRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all registered ETL pipeline engine types (e.g., "BatchCopy", "Streaming"), sourced from
    /// the server-side <c>EtlPipelineTypes</c> ServiceTypeCollection — never hardcoded client-side.
    /// </summary>
    Task<IGenericResult<IReadOnlyList<PipelineTypeSummary>>> GetPipelineTypes(CancellationToken cancellationToken = default);
}
