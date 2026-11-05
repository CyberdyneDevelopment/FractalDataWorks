using Fdw.Operations.Abstractions.Escalation;

namespace Fdw.Operations.Escalation;

/// <summary>The level an alert is due to reach (FDW-3863).</summary>
/// <param name="Level">The level's number.</param>
/// <param name="Definition">The level as the policy states it: delay, channel, recipients, message template.</param>
public sealed record EscalationStep(int Level, IEscalationLevel Definition);
