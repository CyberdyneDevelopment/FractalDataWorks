using System.Collections.Generic;

namespace Fdw.Operations.Abstractions.Thresholds;

/// <summary>A rule that was not judged because a value it names was not reported (FDW-3936). This is not a match and not a clear.</summary>
/// <param name="ThresholdName">The threshold.</param>
/// <param name="RuleNumber">The rule, counted from 1.</param>
/// <param name="Fields">The fields the rule names that the subject did not report.</param>
public sealed record ThresholdNotMeasured(string ThresholdName, int RuleNumber, IReadOnlyList<string> Fields);
