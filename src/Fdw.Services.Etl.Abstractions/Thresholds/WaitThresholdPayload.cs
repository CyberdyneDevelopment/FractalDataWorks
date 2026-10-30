using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Etl.Abstractions.Thresholds;

/// <summary>
/// A threshold on waiting pipeline runs as the API states it (FDW-3161). A save refuses a threshold that leaves a member unstated, naming it; nothing is filled in.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class WaitThresholdPayload
{
    /// <summary>Gets or sets the threshold's name, unique among the thresholds of its level.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets the pipeline whose waiting runs are counted; none counts the waiting runs of the whole server.</summary>
    public string? CountedPipeline { get; set; }

    /// <summary>Gets or sets how the count is compared with the number: a name of the type collection <c>ComparisonOperators</c>.</summary>
    public string? Comparison { get; set; }

    /// <summary>Gets or sets the number the count is compared with; 0 or more.</summary>
    public int? Number { get; set; }

    /// <summary>Gets or sets the severity of the alert: a name of the type collection <c>AlertSeverities</c>.</summary>
    public string? Severity { get; set; }

    /// <summary>Gets or sets the names of the notification rules sent when the threshold fires.</summary>
    public IList<string> NotificationRules { get; set; } = [];

    /// <summary>Gets or sets whether a run that arrives while the threshold fires is not taken.</summary>
    public bool? StopsNewRuns { get; set; }

    /// <summary>Gets or sets whether the threshold is judged at all.</summary>
    public bool? IsEnabled { get; set; }
}
