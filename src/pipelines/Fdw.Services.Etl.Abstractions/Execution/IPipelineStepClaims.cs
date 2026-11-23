using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Etl.Abstractions.Execution;

/// <summary>
/// The claims of the steps that act outside the run (FDW-6139): a step is claimed before it acts, so a restart, a retry or a repeated answer that reaches it again finds it claimed and does not act twice.
/// </summary>
public interface IPipelineStepClaims
{
    /// <summary>Claims one step of one iteration of a chain of runs.</summary>
    /// <param name="tenantId">The run's tenant.</param>
    /// <param name="claimRootId">The first run of the chain of restarts the run belongs to (<c>FlowRunIdentity.ClaimRootId</c>).</param>
    /// <param name="component">The component's name.</param>
    /// <param name="iteration">How many times the step acted in this run before this one, so a step in a loop is claimed once for each round.</param>
    /// <param name="executionId">The run that claims.</param>
    /// <param name="cancellationToken">Cancels the claim.</param>
    /// <returns>The claim: new when this call made it, otherwise the one made before with its outcome; or the failure that says the claim could not be made or read.</returns>
    Task<IGenericResult<StepClaim>> Claim(Guid tenantId, Guid claimRootId, string component, int iteration, Guid executionId, CancellationToken cancellationToken = default);

    /// <summary>Records how a claimed step ended.</summary>
    /// <param name="tenantId">The run's tenant.</param>
    /// <param name="claimId">The claim's id.</param>
    /// <param name="outcome">One of <see cref="StepClaimOutcomes"/>.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>Success, or the failure that says the outcome was not recorded.</returns>
    Task<IGenericResult> Record(Guid tenantId, Guid claimId, string outcome, CancellationToken cancellationToken = default);
}
