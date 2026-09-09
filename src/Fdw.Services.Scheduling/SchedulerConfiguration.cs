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
    /// <summary>Gets or sets the durable logical identifier (matches sched.Scheduler.Id).</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name of this scheduler for lookup and display.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets the domain this record belongs to.</summary>
    public string Domain => "Scheduler";

    /// <summary>Gets or sets the implementation this scheduler is (e.g. "Default", "Quartz").</summary>
    public string? Implementation { get; set; }

    /// <inheritdoc/>
    public string? Description { get; set; }

    /// <inheritdoc/>
    public ISchedulerImplementationConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    /// <remarks>
    /// The non-generic view of <see cref="Configuration"/>. The platform service provider reads the
    /// implementation without naming this domain's implementation contract; netstandard2.0 rules out
    /// a default interface implementation, so each domain record states it.
    /// </remarks>
    IGenericConfiguration? IDomainConfiguration.ImplementationConfiguration => Configuration;

}
