using Fdw.Collections;

namespace Fdw.Operations.Abstractions.Escalation;

/// <summary>Base of the <see cref="EscalationOpenOutcomes"/> options.</summary>
public abstract class EscalationOpenOutcomeBase : TypeOptionBase<EscalationOpenOutcomeBase>, IEscalationOpenOutcome
{
    /// <summary>Initializes a new instance of the <see cref="EscalationOpenOutcomeBase"/> class.</summary>
    /// <param name="id">The option's id, unique in the collection.</param>
    /// <param name="name">The option's name.</param>
    protected EscalationOpenOutcomeBase(int id, string name)
        : base(id, name)
    {
    }
}
