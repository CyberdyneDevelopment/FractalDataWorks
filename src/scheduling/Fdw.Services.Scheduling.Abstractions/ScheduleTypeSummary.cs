namespace Fdw.Services.Scheduling.Clients.Abstractions;

/// <summary>
/// Summary information about a schedule type for UI pickers.
/// </summary>
/// <remarks>
/// Returned by GET /schedules/types, mapped from <c>TriggerTypeBase</c> so the UI picker is
/// driven by the source-generated TypeCollection that also evaluates due-ness. There was a second
/// collection naming the same concepts without any behaviour behind them, and a schedule had to be
/// translated from one to the other before it could be evaluated; the translation handled one of
/// the six names and silently skipped a schedule whenever it did not match.
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
