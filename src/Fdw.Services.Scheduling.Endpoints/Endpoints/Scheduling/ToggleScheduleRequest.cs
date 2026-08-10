using System.ComponentModel.DataAnnotations;

namespace Fdw.Services.Scheduling.Endpoints;

/// <summary>
/// Request to toggle a schedule's enabled/disabled status.
/// </summary>
public class ToggleScheduleRequest
{
    /// <summary>Gets or sets the schedule name (bound from route).</summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the desired enabled status.</summary>
    public bool IsEnabled { get; set; }
}
