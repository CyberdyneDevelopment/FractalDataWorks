using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.Services.Etl.Abstractions.Thresholds;

/// <summary>
/// Every threshold on waiting runs the host is configured with (FDW-3161): what the server states, and what each pipeline and each schedule states. A pipeline or a
/// schedule is found by name, ignoring case.
/// </summary>
public sealed class WaitThresholdConfiguration
{
    /// <summary>Initializes a new instance of the <see cref="WaitThresholdConfiguration"/> class.</summary>
    /// <param name="server">What the server states.</param>
    /// <param name="pipelines">What each pipeline states, by pipeline name.</param>
    /// <param name="schedules">What each schedule states, by schedule name.</param>
    public WaitThresholdConfiguration(
        WaitThresholdLayer server, IReadOnlyDictionary<string, WaitThresholdLayer> pipelines, IReadOnlyDictionary<string, WaitThresholdLayer> schedules)
    {
        Server = server ?? throw new ArgumentNullException(nameof(server));
        Pipelines = (pipelines ?? throw new ArgumentNullException(nameof(pipelines))).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        Schedules = (schedules ?? throw new ArgumentNullException(nameof(schedules))).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Gets what the server states.</summary>
    public WaitThresholdLayer Server { get; }

    /// <summary>Gets what each pipeline states, by pipeline name, ignoring case.</summary>
    public IReadOnlyDictionary<string, WaitThresholdLayer> Pipelines { get; }

    /// <summary>Gets what each schedule states, by schedule name, ignoring case.</summary>
    public IReadOnlyDictionary<string, WaitThresholdLayer> Schedules { get; }

    /// <summary>Lists every threshold that is stated at any level.</summary>
    /// <returns>The thresholds of the server, the pipelines and the schedules.</returns>
    public IReadOnlyList<WaitThreshold> All() =>
        [.. Server.Thresholds, .. Pipelines.Values.SelectMany(layer => layer.Thresholds), .. Schedules.Values.SelectMany(layer => layer.Thresholds)];
}
