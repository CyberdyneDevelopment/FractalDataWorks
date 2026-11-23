using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Pipelines.Abstractions;

namespace Fdw.Services.Etl.Abstractions;

/// <summary>
/// Represents an ETL pipeline that extracts, transforms, and loads data: the ETL closing of <see cref="IPipeline{TIn, TOut}"/> (FDW-6244), which takes
/// the execution options (production or test mode) and answers with the execution result. A run with explicit options is
/// <see cref="IPipeline{TIn, TOut}.Execute(TIn, CancellationToken)"/>.
/// </summary>
public interface IEtlPipeline : IPipeline<PipelineExecutionOptions, IEtlPipelineExecutionResult>, IServiceOption
{
    /// <summary>
    /// Executes the pipeline in production mode.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of pipeline execution.</returns>
    Task<IGenericResult<IEtlPipelineExecutionResult>> Execute(CancellationToken cancellationToken = default);
}
