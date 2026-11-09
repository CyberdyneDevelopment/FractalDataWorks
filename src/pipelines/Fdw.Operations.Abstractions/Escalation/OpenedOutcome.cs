using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.Escalation;

/// <summary>A new alert was opened and its levels will be walked.</summary>
[TypeOption(typeof(EscalationOpenOutcomes), "Opened")]
public sealed class OpenedOutcome : EscalationOpenOutcomeBase
{
    /// <summary>Initializes a new instance of the <see cref="OpenedOutcome"/> class.</summary>
    public OpenedOutcome()
        : base(1, "Opened")
    {
    }
}
