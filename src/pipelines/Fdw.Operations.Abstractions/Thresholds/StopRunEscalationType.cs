using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Thresholds;

/// <summary>Stops the run the values were measured in; its records end Cancelled and the run message carries the reason.</summary>
[TypeOption(typeof(EscalationTypes), "StopRun")]
public sealed class StopRunEscalationType : EscalationTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="StopRunEscalationType"/> class.</summary>
    public StopRunEscalationType() : base("StopRun", "Stops the run the values were measured in; its records end Cancelled and the run message carries the reason.", needsPolicy: false, needsNotificationRules: false, actsOnRun: true)
    {
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult> Handle(EscalationFiring firing, IServiceProvider services, CancellationToken cancellationToken = default)
    {
        if (firing is null)
        {
            throw new ArgumentNullException(nameof(firing));
        }
        if (firing.Subject.RunId is not { } runId)
        {
            return EscalationFailures.RunMissing(firing, Name);
        }

        if (EscalationServices.Find<IRunStopper>(services) is not { } stopper)
        {
            return EscalationFailures.ServiceMissing(firing, Name, nameof(IRunStopper));
        }

        return await stopper.Stop(runId, firing.Reason, cancellationToken).ConfigureAwait(false);
    }
}
