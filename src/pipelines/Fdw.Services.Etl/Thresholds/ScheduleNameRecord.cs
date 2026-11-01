using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Etl.Thresholds;

/// <summary>A row of <c>sched.Schedule</c> read for its name, to find the schedule that a run names (FDW-3161).</summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class ScheduleNameRecord
{
    /// <summary>Gets or sets the row's number, which the schedule's implementation row points at.</summary>
    public int RowId { get; set; }

    /// <summary>Gets or sets the schedule's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the row is the current version.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Gets or sets whether the row is deleted.</summary>
    public bool IsDeleted { get; set; }
}
