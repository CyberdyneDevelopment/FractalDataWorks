using System.Collections.Generic;
using Fdw.Etl.Abstractions.Monitoring.AlertSeverityOptions;
using Fdw.Etl.Abstractions.Monitoring.ComparisonOperatorOptions;

namespace Fdw.Services.Etl.Abstractions.Thresholds;

/// <summary>
/// A threshold on waiting pipeline runs (FDW-3161): it counts the waiting runs of the whole server or of one pipeline, fires when the count meets its
/// comparison with its number, and does what it states: sends notification rules and/or stops new runs. Every part of it is read from configuration.
/// </summary>
public sealed class WaitThreshold
{
    /// <summary>Gets who states the threshold: the server, a pipeline or a schedule, by the name the layer carries.</summary>
    public required string Owner { get; init; }

    /// <summary>Gets the threshold's name, which is unique among those its owner states.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the pipeline whose waiting runs are counted; none counts the waiting runs of the whole server.</summary>
    public string? CountedPipeline { get; init; }

    /// <summary>Gets how the count is compared with the number.</summary>
    public required IComparisonOperator Comparison { get; init; }

    /// <summary>Gets the number the count is compared with.</summary>
    public required int Number { get; init; }

    /// <summary>Gets the severity of the alert the threshold raises.</summary>
    public required IAlertSeverity Severity { get; init; }

    /// <summary>Gets the names of the notification rules sent when the threshold fires.</summary>
    public required IReadOnlyList<string> NotificationRules { get; init; }

    /// <summary>Gets whether a run that arrives while the threshold fires is not taken.</summary>
    public required bool StopsNewRuns { get; init; }

    /// <summary>Gets whether the threshold is judged at all; one that is off never fires.</summary>
    public required bool IsEnabled { get; init; }
}
