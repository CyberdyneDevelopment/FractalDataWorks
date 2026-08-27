using System;
using Fdw.Services.Scheduling.Abstractions;

namespace Fdw.Services.Scheduling;

/// <summary>
/// The default scheduler's own configuration.
/// </summary>
public sealed partial class DefaultSchedulerConfiguration : ISchedulerImplementationConfiguration
{
    /// <inheritdoc/>
    public Guid Id { get; set; }

    /// <inheritdoc/>
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string SectionName => "Schedulers:Default";

    /// <inheritdoc/>
    public string ServiceType => "Scheduler";

    /// <inheritdoc/>
    public string? ServiceOptionType { get; set; }

    /// <summary>Gets or sets the owning scheduler's durable id.</summary>
    public Guid SchedulerId { get; set; }

    /// <inheritdoc/>
    public string ScheduleContainerName { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string DataStoreName { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string PathName { get; set; } = string.Empty;
}
