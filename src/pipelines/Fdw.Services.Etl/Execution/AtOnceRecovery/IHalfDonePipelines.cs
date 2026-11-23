using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Etl.Execution.AtOnceRecovery;

/// <summary>Reads the half-done rule a pipeline states (FDW-6986).</summary>
internal interface IHalfDonePipelines
{
    /// <summary>Reads a pipeline's half-done rule.</summary>
    /// <param name="tenantId">The tenant the pipeline belongs to.</param>
    /// <param name="pipelineName">The pipeline.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The rule the pipeline states (null when it states none); a failure when the pipeline cannot be read.</returns>
    Task<IGenericResult<string?>> RuleOf(Guid tenantId, string pipelineName, CancellationToken cancellationToken = default);
}
