using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Etl.Abstractions.Execution;

/// <summary>
/// What a run gave back (FDW-6136): the outputs its pipeline declares, written when the run ends successfully and read by the pipeline that started it and waited.
/// </summary>
public interface IPipelineRunOutputs
{
    /// <summary>Records the outputs of a run that ended successfully; one row for each, so a run records its outputs once.</summary>
    /// <param name="executionId">The run.</param>
    /// <param name="tenantId">The run's tenant.</param>
    /// <param name="outputs">The outputs, as the pipeline declares them.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>Success, or the failure naming what was not written.</returns>
    Task<IGenericResult> Record(Guid executionId, Guid tenantId, IReadOnlyList<PipelineOutputValue> outputs, CancellationToken cancellationToken = default);

    /// <summary>Reads the outputs a run recorded.</summary>
    /// <param name="executionId">The run.</param>
    /// <param name="tenantId">The run's tenant; another tenant's outputs are never read.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The outputs (none for a run that declares none), or the failure naming what was not read.</returns>
    Task<IGenericResult<IReadOnlyList<PipelineOutputValue>>> Read(Guid executionId, Guid tenantId, CancellationToken cancellationToken = default);
}
