using System.ComponentModel.DataAnnotations;

namespace Fdw.Services.Scheduling.Endpoints;

/// <summary>
/// Request identifying a schedule by name.
/// </summary>
public class ScheduleNameRequest
{
    /// <summary>Gets or sets the schedule name.</summary>
    [Required]
    public string Name { get; set; } = string.Empty;
}
