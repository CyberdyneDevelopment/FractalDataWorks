using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.Escalation;

/// <summary>The threshold already has an open alert; no second one was opened and the open one is returned.</summary>
[TypeOption(typeof(EscalationOpenOutcomes), "AlreadyOpen")]
public sealed class AlreadyOpenOutcome : EscalationOpenOutcomeBase
{
    /// <summary>Initializes a new instance of the <see cref="AlreadyOpenOutcome"/> class.</summary>
    public AlreadyOpenOutcome()
        : base(2, "AlreadyOpen")
    {
    }
}
