using System;

namespace Fdw.Services.Scheduling.Clients.Abstractions;

/// <summary>
/// Response from creating a new schedule.
/// </summary>
public class CreateScheduleClientResponse
{
    /// <summary>
    /// Gets or sets the created schedule identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the schedule name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
