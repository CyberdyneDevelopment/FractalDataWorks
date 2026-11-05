using Fdw.Results;

namespace Fdw.Operations.Abstractions.Thresholds;

/// <summary>A rule that matched and what its consequence came to (FDW-3936).</summary>
/// <param name="ThresholdName">The threshold.</param>
/// <param name="RuleNumber">The rule, counted from 1.</param>
/// <param name="Escalation">The consequence that was fired.</param>
/// <param name="Reason">The sentence saying which rule matched and the values it named.</param>
/// <param name="Result">Success when the consequence acted; the failure when it could not.</param>
public sealed record ThresholdFired(string ThresholdName, int RuleNumber, string Escalation, string Reason, IGenericResult Result);
