using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// A step that acts outside the run, claimed before it acts (FDW-6139). The claim is made under the first run of the chain of restarts, the component and the iteration, so a restart, a retry or a repeated
/// answer that reaches the step again does not act again: a step that finished is taken as done, a step that failed fails again with the same cause, and a step that never recorded how it ended stops the
/// run, because nobody can say whether its effect happened. The claim is kept when the step fails.
/// </summary>
/// <remarks>
/// Why no claim without an identity: the claim is made for a run, a tenant and a chain, so a step that has none fails and says so; no claim is assumed. A run that was not tracked is never restarted, and a host
/// that runs pipelines always tracks them.
/// </remarks>
public class FlowClaimedStep : IFlowStep
{
    private readonly IPipelineStepClaims _claims;
    private readonly ILogger _logger;
    private int _iteration = -1;

    /// <summary>Initializes a new instance of the <see cref="FlowClaimedStep"/> class.</summary>
    /// <param name="inner">The step that acts.</param>
    /// <param name="claims">Makes the claims.</param>
    /// <param name="logger">The logger.</param>
    public FlowClaimedStep(IFlowStep inner, IPipelineStepClaims claims, ILogger logger)
    {
        Inner = inner;
        _claims = claims;
        _logger = logger;
    }

    /// <summary>Gets the step that acts.</summary>
    public IFlowStep Inner { get; }

    /// <inheritdoc />
    public string Name => Inner.Name;

    /// <inheritdoc />
    public Task<IGenericResult> Run(FlowStepContext context, CancellationToken cancellationToken = default) =>
        Claimed(context, Name, () => Inner.Run(context, cancellationToken), cancellationToken);

    /// <summary>Claims the step under a name, acts when the claim is new and records how it ended.</summary>
    /// <param name="context">What the run carries.</param>
    /// <param name="claimName">The component name the claim is made under.</param>
    /// <param name="act">What the step does.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>What the step answered, or why it did not act.</returns>
    protected async Task<IGenericResult> Claimed(FlowStepContext context, string claimName, Func<Task<IGenericResult>> act, CancellationToken cancellationToken)
    {
        if (context.Identity is not { } identity)
        {
            return GenericResult.Failure(PipelineStepClaimLog.CannotClaim(_logger, Name, "the run has no identity, so there is no run, tenant or chain to claim under"));
        }

        var claim = await _claims.Claim(identity.TenantId, identity.ClaimRootId, claimName, Interlocked.Increment(ref _iteration), identity.ExecutionId, cancellationToken).ConfigureAwait(false);
        if (claim.IsFailure || claim.Value is not { } made)
        {
            return claim;
        }

        if (!made.IsNew)
        {
            return Taken(made);
        }

        var acted = await act().ConfigureAwait(false);
        var recorded = await _claims.Record(identity.TenantId, made.Id, acted.IsSuccess ? StepClaimOutcomes.Succeeded : StepClaimOutcomes.Failed, cancellationToken).ConfigureAwait(false);
        return recorded.IsFailure ? recorded : acted;
    }

    private IGenericResult Taken(StepClaim claim)
    {
        if (string.Equals(claim.Outcome, StepClaimOutcomes.Succeeded, StringComparison.Ordinal))
        {
            _ = PipelineStepClaimLog.TakenAsDone(_logger, Name, claim.ExecutionId);
            return GenericResult.Success();
        }

        return string.Equals(claim.Outcome, StepClaimOutcomes.Failed, StringComparison.Ordinal)
            ? GenericResult.Failure(PipelineStepClaimLog.ClaimedAndFailed(_logger, Name, claim.ExecutionId))
            : GenericResult.Failure(PipelineStepClaimLog.ClaimedWithNoOutcome(_logger, Name, claim.ExecutionId));
    }
}
