using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Thresholds;

/// <summary>
/// Refuses new runs while the rule's criteria hold (FDW-3936). A hold is a state, not an event, so firing it does nothing: the gate that admits runs asks
/// <see cref="IThresholdEvaluator.Holds"/> whether any rule naming this consequence matches, and refuses the run when one does.
/// </summary>
[TypeOption(typeof(EscalationTypes), "HoldNewRuns")]
public sealed class HoldNewRunsEscalationType : EscalationTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="HoldNewRunsEscalationType"/> class.</summary>
    public HoldNewRunsEscalationType()
        : base("HoldNewRuns", "Refuses new runs while the criteria hold; the gate that admits runs asks, so nothing happens when it fires.", needsPolicy: false, needsNotificationRules: false, actsOnRun: false)
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult> Handle(EscalationFiring firing, IServiceProvider services, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GenericResult.Success());
    }
}
