using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Thresholds;

/// <summary>Raises an alert on the escalation policy the rule names; the policy walks its levels until a person acknowledges or resolves it.</summary>
[TypeOption(typeof(EscalationTypes), "Alert")]
public sealed class AlertEscalationType : EscalationTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="AlertEscalationType"/> class.</summary>
    public AlertEscalationType() : base("Alert", "Raises an alert on the escalation policy the rule names; the policy walks its levels until a person acknowledges or resolves it.", needsPolicy: true, needsNotificationRules: false, actsOnRun: false)
    {
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult> Handle(EscalationFiring firing, IServiceProvider services, CancellationToken cancellationToken = default)
    {
        if (firing.Rule.EscalationPolicy is not { Length: > 0 } policy)
        {
            return EscalationFailures.PolicyMissing(firing, Name);
        }

        if (EscalationServices.Find<Escalation.IEscalationAlerts>(services) is not { } alerts)
        {
            return EscalationFailures.ServiceMissing(firing, Name, nameof(Escalation.IEscalationAlerts));
        }

        var opened = await alerts.Open(
            new Escalation.EscalationAlertRequest(
                firing.Subject.TenantId, firing.Subject.Kind, firing.Subject.Name, firing.ThresholdName, policy,
                $"{firing.ThresholdName} fired on {firing.Subject.Kind} '{firing.Subject.Name}'", firing.Reason),
            cancellationToken).ConfigureAwait(false);
        return opened.IsFailure ? opened : GenericResult.Success();
    }
}
