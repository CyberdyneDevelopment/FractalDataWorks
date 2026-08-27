using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Abstractions;
using Fdw.Services.Scheduling.Abstractions;
using Fdw.Services.Scheduling.Abstractions.Configuration;

namespace Fdw.Services.Scheduling;

/// <summary>
/// Header configuration for scheduling services representing the sched.Scheduler parent table.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Scheduler")]
public partial class SchedulerConfiguration : ISchedulerConfiguration
{
    /// <summary>
    /// Gets or sets the unique identifier for this scheduler.
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Gets or sets the name of this scheduler for lookup and display.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets the section name for configuration binding.
    /// </summary>
    public string SectionName => "Scheduler";

    /// <summary>
    /// Gets the service type (domain) - always "Scheduler" for this configuration.
    /// </summary>
    public string ServiceType => "Scheduler";

    /// <summary>
    /// Gets or sets the name of the DataSet container for schedule data access.
    /// </summary>
    public string ScheduleContainerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the DataStore name for schedule data access (required).
    /// </summary>
    public string DataStoreName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the path name (schema) within the DataStore (required).
    /// </summary>
    public string PathName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the service option type (e.g., "Quartz", "Hangfire").
    /// </summary>
    public string? ServiceOptionType { get; set; }

    /// <summary>
    /// Gets the scheduling type name. Alias for <see cref="ServiceOptionType"/>.
    /// </summary>
    public string? SchedulingType => ServiceOptionType;

    /// <inheritdoc />
    public IServiceLifetime Lifetime { get; set; } = ServiceLifetimes.Singleton;

    /// <inheritdoc />
    public string? SecretManagerName { get; set; }

    /// <inheritdoc />
    public string? SecretKeyName { get; set; }

    /// <inheritdoc />
    public int MaxConcurrency { get; set; } = 10;

    /// <inheritdoc />
    public int DefaultTimeoutSeconds { get; set; } = 3600;

    /// <inheritdoc />
    public bool PersistJobHistory { get; set; }

    /// <inheritdoc />
    public string? PersistenceConnectionString { get; set; }

    /// <inheritdoc />
    public bool EnableClustering { get; set; }

    /// <inheritdoc />
    public string? ClusterInstanceId { get; set; }

    /// <inheritdoc />
    public int MisfireThresholdSeconds { get; set; } = 60;

    /// <inheritdoc />
    public bool EnableDetailedLogging { get; set; }

    /// <summary>
    /// Gets or sets the optional description of this scheduling.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the nested schedules collection.
    /// </summary>
    public IList<ScheduleConfiguration> Schedules { get; set; } = [];

}
