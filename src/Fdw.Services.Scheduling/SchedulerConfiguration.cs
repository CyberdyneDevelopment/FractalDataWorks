using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Scheduling.Abstractions;

namespace Fdw.Services.Scheduling;

/// <summary>
/// One configured scheduler — the <c>sched.Scheduler</c> domain record, naming which implementation
/// it is and holding that implementation's own configuration.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Scheduler")]
public partial class SchedulerConfiguration : ISchedulerConfiguration
{
    // Why no generated default: the database assigns identity. A value minted here reaches
    // Get(domainId) as a real-looking id that matches no row, and the miss reads as a data problem
    // rather than an unsaved record.
    /// <summary>Gets or sets the durable logical identifier (matches sched.Scheduler.Id).</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name of this scheduler for lookup and display.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets the section name for configuration binding.</summary>
    public string SectionName => "Scheduler";

    /// <summary>Gets the service type (domain).</summary>
    public string ServiceType => "Scheduler";

    /// <summary>Gets or sets the implementation this scheduler is (e.g. "Default", "Quartz").</summary>
    public string? ServiceOptionType { get; set; }

    /// <inheritdoc/>
    public string? Description { get; set; }

    /// <inheritdoc/>
    public ISchedulerImplementationConfiguration? Configuration { get; set; }
}
