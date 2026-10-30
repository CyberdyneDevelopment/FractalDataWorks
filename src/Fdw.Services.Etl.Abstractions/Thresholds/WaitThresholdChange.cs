using System.Collections.Generic;

namespace Fdw.Services.Etl.Abstractions.Thresholds;

/// <summary>What changed when thresholds on waiting runs were judged (FDW-3161).</summary>
/// <param name="Fired">The thresholds that began to fire.</param>
/// <param name="Cleared">The keys of the thresholds that stopped firing, or that no longer exist.</param>
public sealed record WaitThresholdChange(IReadOnlyList<WaitThreshold> Fired, IReadOnlyList<string> Cleared);
