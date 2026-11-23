using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Thresholds;

/// <summary>Pauses the schedule until a person enables it again; a run it already started finishes (FDW-6144).</summary>
[TypeOption(typeof(EscalationTypes), "PauseSchedule")]
public sealed class PauseScheduleEscalationType : EscalationTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="PauseScheduleEscalationType"/> class.</summary>
    public PauseScheduleEscalationType() : base("PauseSchedule", "Pauses the schedule until a person enables it again; a run it already started finishes.", needsPolicy: false, needsNotificationRules: false, actsOnRun: false)
    {
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult> Handle(EscalationFiring firing, IServiceProvider services, CancellationToken cancellationToken = default)
    {
        if (EscalationServices.Find<ISchedulePauser>(services) is not { } pauser)
        {
            return EscalationFailures.ServiceMissing(firing, Name, nameof(ISchedulePauser));
        }

        return await pauser.Pause(firing, cancellationToken).ConfigureAwait(false);
    }
}
