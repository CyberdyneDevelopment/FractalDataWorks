namespace Fdw.Operations.Abstractions.Thresholds;

/// <summary>A rule whose criteria hold for a set of values (FDW-3936), found without firing anything.</summary>
/// <param name="ThresholdName">The threshold.</param>
/// <param name="RuleNumber">The rule, counted from 1.</param>
/// <param name="Escalation">The consequence the rule names.</param>
/// <param name="Reason">The sentence saying which rule matched and the values it named.</param>
public sealed record ThresholdMatch(string ThresholdName, int RuleNumber, string Escalation, string Reason);
