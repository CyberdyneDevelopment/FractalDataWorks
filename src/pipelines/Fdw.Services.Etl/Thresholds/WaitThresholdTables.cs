using System.Collections.Generic;

namespace Fdw.Services.Etl.Thresholds;

/// <summary>The current rows of the three containers that hold thresholds on waiting runs: the levels, the thresholds and the notification rules each sends (FDW-3161).</summary>
/// <param name="Levels">The level each owner states.</param>
/// <param name="Thresholds">The thresholds.</param>
/// <param name="Rules">The notification rule names of each threshold.</param>
internal sealed record WaitThresholdTables(
    IReadOnlyList<WaitThresholdLevelRecord> Levels, IReadOnlyList<WaitThresholdRecord> Thresholds, IReadOnlyList<WaitThresholdRuleRecord> Rules);
