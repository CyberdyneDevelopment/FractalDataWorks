using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Thresholds;

/// <summary>Sends the notification rules the rule names once, when it matches.</summary>
[TypeOption(typeof(EscalationTypes), "Notify")]
public sealed class NotifyEscalationType : EscalationTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="NotifyEscalationType"/> class.</summary>
    public NotifyEscalationType() : base("Notify", "Sends the notification rules the rule names once, when it matches.", needsPolicy: false, needsNotificationRules: true, actsOnRun: false)
    {
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult> Handle(EscalationFiring firing, IServiceProvider services, CancellationToken cancellationToken = default)
    {
        if (EscalationServices.Find<IThresholdNotifier>(services) is not { } notifier)
        {
            return EscalationFailures.ServiceMissing(firing, Name, nameof(IThresholdNotifier));
        }

        return await notifier.Notify(
            firing.Rule.NotificationRules, $"{firing.ThresholdName} fired on {firing.Subject.Kind} '{firing.Subject.Name}'", firing.Reason,
            new System.Collections.Generic.Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["Threshold"] = firing.ThresholdName, ["SubjectKind"] = firing.Subject.Kind, ["SubjectName"] = firing.Subject.Name, ["Rule"] = firing.RuleNumber,
            },
            cancellationToken).ConfigureAwait(false);
    }
}
