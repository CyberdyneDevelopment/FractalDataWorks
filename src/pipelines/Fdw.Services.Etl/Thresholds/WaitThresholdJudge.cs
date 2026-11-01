using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Fdw.Services.Etl.Abstractions.Thresholds;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Thresholds;

/// <summary>
/// Judges thresholds on waiting runs and remembers which fire (FDW-3161). A threshold fires when its count meets its comparison with its number and clears
/// when it no longer does, so a queue that stays over the number raises one alert and not one for each run that joins it. A threshold that is off never
/// fires, and one that is no longer stated clears.
/// </summary>
public sealed class WaitThresholdJudge
{
    private readonly Lock _gate = new();
    private readonly HashSet<string> _firing = new(StringComparer.Ordinal);
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="WaitThresholdJudge"/> class.</summary>
    /// <param name="logger">The logger.</param>
    public WaitThresholdJudge(ILogger<WaitThresholdJudge>? logger = null)
    {
        _logger = logger ?? NullLogger<WaitThresholdJudge>.Instance;
    }

    /// <summary>Gets the key that names a threshold among all that are stated: its owner and its name.</summary>
    /// <param name="threshold">The threshold.</param>
    /// <returns>The key.</returns>
    public static string KeyOf(WaitThreshold threshold)
    {
        ArgumentNullException.ThrowIfNull(threshold);
        return string.Concat(threshold.Owner, "/", threshold.Name);
    }

    /// <summary>Judges every stated threshold against the counts.</summary>
    /// <param name="thresholds">Every threshold that is stated, at the server, on pipelines and on schedules.</param>
    /// <param name="counts">How many runs wait.</param>
    /// <returns>The thresholds that began to fire and the keys of those that stopped.</returns>
    public WaitThresholdChange Judge(IReadOnlyList<WaitThreshold> thresholds, WaitCounts counts)
    {
        ArgumentNullException.ThrowIfNull(thresholds);
        ArgumentNullException.ThrowIfNull(counts);

        var fired = new List<WaitThreshold>();
        var cleared = new List<string>();
        lock (_gate)
        {
            foreach (var threshold in thresholds)
            {
                var key = KeyOf(threshold);
                var count = counts.Of(threshold.CountedPipeline);
                if (threshold.IsEnabled && threshold.Comparison.Evaluate(count, threshold.Number))
                {
                    if (_firing.Add(key))
                    {
                        fired.Add(threshold);
                        WaitThresholdLog.Fired(_logger, threshold.Name, threshold.Owner, count, threshold.Comparison.Name, threshold.Number);
                    }
                }
                else if (_firing.Remove(key))
                {
                    cleared.Add(key);
                    WaitThresholdLog.Cleared(_logger, key);
                }
            }

            var stated = thresholds.Select(KeyOf).ToHashSet(StringComparer.Ordinal);
            foreach (var gone in _firing.Where(key => !stated.Contains(key)).ToList())
            {
                _firing.Remove(gone);
                cleared.Add(gone);
                WaitThresholdLog.Cleared(_logger, gone);
            }
        }

        return new WaitThresholdChange(fired, cleared);
    }

    /// <summary>
    /// Finds a threshold among the given that stops new runs and whose condition holds for the given counts. It judges the counts as they are now and not the thresholds
    /// that were found firing at the last change, so a threshold saved while its condition already holds stops the very next run.
    /// </summary>
    /// <param name="effective">The thresholds that apply to a run that arrives.</param>
    /// <param name="counts">The runs that wait now.</param>
    /// <returns>The first threshold that stops new runs and holds; none when no such threshold holds.</returns>
    public static WaitThreshold? Stopping(IReadOnlyList<WaitThreshold> effective, WaitCounts counts)
    {
        ArgumentNullException.ThrowIfNull(effective);
        ArgumentNullException.ThrowIfNull(counts);
        return effective.FirstOrDefault(threshold => threshold.StopsNewRuns && threshold.IsEnabled && threshold.Comparison.Evaluate(counts.Of(threshold.CountedPipeline), threshold.Number));
    }
}
