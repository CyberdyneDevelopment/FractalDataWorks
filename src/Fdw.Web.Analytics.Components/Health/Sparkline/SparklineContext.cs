using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.Web.Analytics.Clients.Models;
using Fdw.Web.Analytics.Components.Health.TrendDirectionOptions;
using Fdw.UI.Providers;

namespace Fdw.Web.Analytics.Components.Health.Sparkline;

/// <summary>
/// Immutable context for the Sparkline headless provider.
/// </summary>
// Why: pure view-model — state + callback delegates only, no logic.
[ExcludeFromCodeCoverage]
public sealed class SparklineContext : ProviderContextBase
{
    /// <summary>Gets the time series data points.</summary>
    public IReadOnlyList<TimeSeriesDataPoint> DataPoints { get; init; } = [];

    /// <summary>Gets the minimum value in the series.</summary>
    public double Min { get; init; }

    /// <summary>Gets the maximum value in the series.</summary>
    public double Max { get; init; }

    /// <summary>Gets the label text.</summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>Gets the duration of the time window.</summary>
    public TimeSpan Duration { get; init; }

    /// <summary>Gets the trend direction.</summary>
    public ITrendDirection TrendDirection { get; init; } = TrendDirections.ByName("Flat");

    /// <summary>Gets the percentage change over the window.</summary>
    public double ChangePercent { get; init; }



}
