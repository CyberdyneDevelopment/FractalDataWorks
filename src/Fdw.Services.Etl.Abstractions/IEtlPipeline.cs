using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Pipelines.Abstractions;

namespace Fdw.Services.Etl.Abstractions;

/// <summary>
/// Represents an ETL pipeline that extracts, transforms, and loads data. The ETL KIND of the general
/// <see cref="IPipeline"/> runtime base.
/// </summary>
public interface IEtlPipeline : IPipeline, IServiceOption
{
    /// <summary>
    /// Executes the pipeline in production mode.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of pipeline execution.</returns>
    Task<IGenericResult<IEtlPipelineExecutionResult>> Execute(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the pipeline with explicit execution options (production or test mode).
    /// </summary>
    /// <param name="options">Execution options controlling test caps, sample buffers, and broadcast cadence.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of pipeline execution.</returns>
    Task<IGenericResult<IEtlPipelineExecutionResult>> Execute(PipelineExecutionOptions options, CancellationToken cancellationToken = default);
}
