using Fdw.Collections;

namespace Fdw.Operations.Abstractions.Escalation;

/// <summary>What opening an alert came to (FDW-3863): one option of <see cref="EscalationOpenOutcomes"/>.</summary>
public interface IEscalationOpenOutcome : ITypeOption<int, IEscalationOpenOutcome>
{
}
