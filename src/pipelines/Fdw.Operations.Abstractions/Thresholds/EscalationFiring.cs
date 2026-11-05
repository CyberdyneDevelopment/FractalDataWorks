using System.Collections.Generic;

namespace Fdw.Operations.Abstractions.Thresholds;

/// <summary>What a consequence is handed when its rule matched (FDW-3936).</summary>
/// <param name="Subject">What the values are about.</param>
/// <param name="ThresholdName">The threshold whose rule matched.</param>
/// <param name="RuleNumber">The rule, counted from 1 in the order they are stated.</param>
/// <param name="Rule">The rule that matched.</param>
/// <param name="Values">The values the subject reported.</param>
/// <param name="Reason">A sentence saying which rule matched and the values it named.</param>
public sealed record EscalationFiring(
    ThresholdSubject Subject, string ThresholdName, int RuleNumber, ThresholdRule Rule, IReadOnlyDictionary<string, object?> Values, string Reason);
