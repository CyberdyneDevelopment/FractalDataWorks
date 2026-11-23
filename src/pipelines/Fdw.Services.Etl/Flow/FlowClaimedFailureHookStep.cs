using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Abstractions.Flow;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// A claimed step that also does its work for a run that failed (FDW-6139): what it does then is an effect too, so it is claimed under its own name and counted apart from what it did when the run reached it.
/// </summary>
public sealed class FlowClaimedFailureHookStep : FlowClaimedStep, IFlowFailureHook
{
    private const string FailureSuffix = "/failure";

    private readonly IFlowFailureHook _hook;

    /// <summary>Initializes a new instance of the <see cref="FlowClaimedFailureHookStep"/> class.</summary>
    /// <param name="inner">The step that acts, which also acts for a run that failed.</param>
    /// <param name="claims">Makes the claims.</param>
    /// <param name="logger">The logger.</param>
    public FlowClaimedFailureHookStep(IFlowFailureHook inner, IPipelineStepClaims claims, ILogger logger)
        : base(inner, claims, logger)
    {
        _hook = inner;
    }

    /// <inheritdoc />
    public Task<IGenericResult> OnFailure(FlowStepContext context, IGenericResult failure, CancellationToken cancellationToken = default) =>
        Claimed(context, Name + FailureSuffix, () => _hook.OnFailure(context, failure, cancellationToken), cancellationToken);
}
