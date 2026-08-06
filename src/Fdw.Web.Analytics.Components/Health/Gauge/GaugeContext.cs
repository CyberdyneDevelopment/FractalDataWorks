using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.Services.Abstractions.Health;
using Fdw.UI.Providers;

namespace Fdw.Web.Analytics.Components.Health.Gauge;

/// <summary>
/// Immutable context for the Gauge headless provider.
/// </summary>
// Why: pure view-model — state + callback delegates only, no logic.
[ExcludeFromCodeCoverage]
public sealed class GaugeContext : ProviderContextBase
{
    /// <summary>Gets the current value.</summary>
    public double Value { get; init; }

    /// <summary>Gets the minimum value of the gauge range.</summary>
    public double Min { get; init; }

    /// <summary>Gets the maximum value of the gauge range.</summary>
    public double Max { get; init; } = 100;

    /// <summary>Gets the label text.</summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>Gets the unit suffix (e.g., "%", "ms").</summary>
    public string Unit { get; init; } = string.Empty;

    /// <summary>Gets the current health status.</summary>
    public IHealthState? Status { get; init; }

    /// <summary>Gets the warning threshold value.</summary>
    public double WarningThreshold { get; init; }

    /// <summary>Gets the critical threshold value.</summary>
    public double CriticalThreshold { get; init; }



}
