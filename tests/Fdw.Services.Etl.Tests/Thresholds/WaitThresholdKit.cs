using System.Collections.Generic;
using Fdw.Etl.Abstractions.Monitoring.AlertSeverityOptions;
using Fdw.Etl.Abstractions.Monitoring.ComparisonOperatorOptions;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Abstractions.Thresholds;

namespace Fdw.Services.Etl.Tests.Thresholds;

/// <summary>Builds the thresholds and levels the threshold tests judge (FDW-3161).</summary>
internal static class WaitThresholdKit
{
    public static WaitThreshold Threshold(
        string name, int number, string owner = "Server", string? pipeline = null, string comparison = "GreaterThanOrEqual", bool stops = false, bool enabled = true,
        params string[] rules) => new()
        {
            Owner = owner,
            Name = name,
            CountedPipeline = pipeline,
            Comparison = ComparisonOperators.ByName(comparison),
            Number = number,
            Severity = AlertSeverities.ByName("Warning"),
            NotificationRules = rules,
            StopsNewRuns = stops,
            IsEnabled = enabled,
        };

    public static WaitThresholdLayer Layer(string owner, string mode, params WaitThreshold[] thresholds) =>
        new(owner, WaitThresholdModes.ByName(mode), thresholds);

    public static WaitCounts Counts(int server, params (string Pipeline, int Waiting)[] byPipeline)
    {
        var map = new Dictionary<string, int>(System.StringComparer.Ordinal);
        foreach (var (pipeline, waiting) in byPipeline)
        {
            map[pipeline] = waiting;
        }

        return new WaitCounts(server, map);
    }
}
