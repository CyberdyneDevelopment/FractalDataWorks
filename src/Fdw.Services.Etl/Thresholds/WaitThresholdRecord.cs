using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Etl.Thresholds;

/// <summary>A row of <c>pipe.WaitThreshold</c>: a threshold on waiting pipeline runs (FDW-3161).</summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class WaitThresholdRecord
{
    /// <summary>Gets or sets the threshold's durable id.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets who states the threshold: <c>Server</c>, <c>Pipeline</c> or <c>Schedule</c>.</summary>
    public string OwnerKind { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the pipeline or schedule that states the threshold; none for the server.</summary>
    public string? OwnerName { get; set; }

    /// <summary>Gets or sets the threshold's name, unique among those its owner states.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the pipeline whose waiting runs are counted; none counts the whole server's.</summary>
    public string? CountedPipeline { get; set; }

    /// <summary>Gets or sets the name of the comparison, an option of <c>ComparisonOperators</c>.</summary>
    public string Comparison { get; set; } = string.Empty;

    /// <summary>Gets or sets the number the count is compared with.</summary>
    public int Number { get; set; }

    /// <summary>Gets or sets the name of the severity, an option of <c>AlertSeverities</c>.</summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>Gets or sets whether a run that arrives while the threshold fires is not taken.</summary>
    public bool StopsNewRuns { get; set; }

    /// <summary>Gets or sets whether the threshold is judged at all.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Gets or sets whether the row is the current version.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Gets or sets whether the row is deleted.</summary>
    public bool IsDeleted { get; set; }
}
