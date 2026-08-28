using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.Web.Analytics.Clients.Models;
using Fdw.Web.Analytics.Components.Health.TrendDirectionOptions;
using Fdw.UI.Providers;

namespace Fdw.Web.Analytics.Components.Health.Throughput;

/// <summary>
/// Immutable context for the Throughput headless provider.
/// Combines gauge and sparkline data for throughput visualization.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ThroughputContext : ProviderContextBase
{
    /// <summary>Gets the current requests per second rate.</summary>
    public double RequestsPerSecond { get; init; }

    /// <summary>Gets the average latency in milliseconds.</summary>
    public double AvgLatencyMs { get; init; }

    /// <summary>Gets the 95th percentile latency in milliseconds.</summary>
    public double P95LatencyMs { get; init; }

    /// <summary>Gets the error rate as a fraction (0.0 to 1.0).</summary>
    public double ErrorRate { get; init; }

    /// <summary>Gets the history sparkline data points for latency trend.</summary>
    public IReadOnlyList<TimeSeriesDataPoint> HistoryDataPoints { get; init; } = [];

    /// <summary>Gets the trend direction for latency.</summary>
    public ITrendDirection LatencyTrend { get; init; } = TrendDirections.ByName("Flat");

    /// <summary>Gets the latency change percentage.</summary>
    public double LatencyChangePercent { get; init; }



}
