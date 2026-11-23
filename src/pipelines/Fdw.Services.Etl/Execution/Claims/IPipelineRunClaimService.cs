using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Etl.Execution.Claims;

/// <summary>
/// The durable claim of pipeline runs (FDW-6629): what a run holds so that two runs that write the same table, share a stated key or exceed the cap of their pipeline do not run at once, kept in the
/// operational database so every host and both A/B copies see it. Each call is one of the three OpsDb procedures, called through the data gateway.
/// </summary>
public interface IPipelineRunClaimService
{
    /// <summary>Claims the keys of a run, or asks again for a run that waits. A run that claims no key is claimed at once without a call.</summary>
    /// <param name="request">The run and its keys.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>Whether the run holds its claim or waits, and the claims of other runs found lapsed; or the failure.</returns>
    Task<IGenericResult<PipelineRunClaimOutcome>> Claim(PipelineRunClaimRequest request, CancellationToken cancellationToken = default);

    /// <summary>Renews the lease of the runs a host and slot hold.</summary>
    /// <param name="executionIds">The runs.</param>
    /// <param name="executingHost">The host that holds them.</param>
    /// <param name="executingSlot">The slot of the host's copy, or null.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>For each run, whether it still holds its claim; and the claims found lapsed; or the failure.</returns>
    Task<IGenericResult<PipelineRunRenewed>> Renew(IReadOnlyList<Guid> executionIds, string executingHost, string? executingSlot, CancellationToken cancellationToken = default);

    /// <summary>Releases the claims of a run and promotes the runs waiting behind it.</summary>
    /// <param name="executionId">The run.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The runs promoted and the claims found lapsed; or the failure.</returns>
    Task<IGenericResult<PipelineRunReleased>> Release(Guid executionId, CancellationToken cancellationToken = default);
}
