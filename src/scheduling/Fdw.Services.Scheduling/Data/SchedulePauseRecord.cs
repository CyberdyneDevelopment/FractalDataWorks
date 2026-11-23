using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Scheduling.Data;

/// <summary>The columns a threshold's pause of a schedule writes on the schedule's current row (FDW-6144): it is disabled, and when and why are kept.</summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed class SchedulePauseRecord
{
    /// <summary>Gets or sets whether the schedule runs; false for a paused schedule.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Gets or sets when the schedule was paused.</summary>
    public DateTimeOffset PausedAt { get; set; }

    /// <summary>Gets or sets why: the threshold, its rule and the values it matched on.</summary>
    public string PausedReason { get; set; } = string.Empty;
}
