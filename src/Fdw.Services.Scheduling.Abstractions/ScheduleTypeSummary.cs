namespace Fdw.Services.Scheduling.Clients.Abstractions;

/// <summary>
/// Summary information about a schedule type for UI pickers.
/// </summary>
/// <remarks>
/// Returned by GET /schedules/types. Maps directly from
/// <see cref="Fdw.Services.Scheduling.Abstractions.TypeCollections.ScheduleTypeOptions.ScheduleTypeBase"/>
/// so the UI picker is driven by the source-generated TypeCollection rather than the
/// broken generic category endpoint.
/// </remarks>
public sealed class ScheduleTypeSummary
{
    /// <summary>Gets or sets the internal type name (e.g., "Cron").</summary>
    public string TypeName { get; set; } = string.Empty;

    /// <summary>Gets or sets the user-friendly display name.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the category (always "Schedule" for schedule types).</summary>
    public string Category { get; set; } = string.Empty;
}
