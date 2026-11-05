using System.Collections.Generic;

namespace Fdw.Operations.Abstractions.Thresholds;

/// <summary>What reporting a subject values came to (FDW-3936).</summary>
/// <param name="ThresholdsJudged">How many enabled thresholds were attached to the subject. A subject with none is not judged, and this is 0.</param>
/// <param name="Fired">The rules that matched, in the order they were judged, with what their consequence came to.</param>
/// <param name="NotMeasured">The rules that were not judged for want of a reported value.</param>
public sealed record ThresholdReport(int ThresholdsJudged, IReadOnlyList<ThresholdFired> Fired, IReadOnlyList<ThresholdNotMeasured> NotMeasured);
