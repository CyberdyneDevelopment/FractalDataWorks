using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.Escalation;

/// <summary>The threshold's latest alert was resolved less than the policy's cooldown ago; no alert was opened.</summary>
[TypeOption(typeof(EscalationOpenOutcomes), "CoolingDown")]
public sealed class CoolingDownOutcome : EscalationOpenOutcomeBase
{
    /// <summary>Initializes a new instance of the <see cref="CoolingDownOutcome"/> class.</summary>
    public CoolingDownOutcome()
        : base(3, "CoolingDown")
    {
    }
}
