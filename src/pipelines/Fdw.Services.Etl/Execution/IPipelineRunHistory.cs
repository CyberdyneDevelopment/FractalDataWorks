using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Etl.Execution;

/// <summary>Reads how a pipeline's finished runs ended, newest first, for the failure rule (FDW-3864).</summary>
public interface IPipelineRunHistory
{
    /// <summary>Reads one page of the pipeline's finished runs.</summary>
    /// <param name="pipelineName">The pipeline.</param>
    /// <param name="skip">How many of the newest runs to skip.</param>
    /// <param name="take">How many runs to read.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The runs, newest first; empty when there are no more.</returns>
    Task<IGenericResult<IReadOnlyList<PipelineRunOutcome>>> Page(string pipelineName, int skip, int take, CancellationToken cancellationToken = default);
}
