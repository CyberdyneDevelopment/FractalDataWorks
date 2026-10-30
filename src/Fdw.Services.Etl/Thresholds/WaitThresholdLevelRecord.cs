using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Etl.Thresholds;

/// <summary>A row of <c>pipe.WaitThresholdLevel</c>: the mode of the level the server, a pipeline or a schedule states (FDW-3161).</summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class WaitThresholdLevelRecord
{
    /// <summary>Gets or sets the row's durable id.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets who states the level: <c>Server</c>, <c>Pipeline</c> or <c>Schedule</c>.</summary>
    public string OwnerKind { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the pipeline or schedule that states the level; none for the server.</summary>
    public string? OwnerName { get; set; }

    /// <summary>Gets or sets the name of the mode, an option of <c>WaitThresholdModes</c>.</summary>
    public string Mode { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the row is the current version.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Gets or sets whether the row is deleted.</summary>
    public bool IsDeleted { get; set; }
}
