using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using Fdw.Services.Scheduling.Abstractions.OptionTypes;

namespace Fdw.Services.Scheduling.Abstractions.Configuration;

/// <summary>
/// Configuration class for all schedule types.
/// Maps to <c>sched.Schedule</c> which contains all schedule fields including type-specific ones.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Schedule")]
public partial class ScheduleConfiguration : DomainConfigurationBase, IScheduleDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduleConfiguration"/> class.
    /// Default constructor for IOptions binding and header lookups.
    /// </summary>
    public ScheduleConfiguration() : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduleConfiguration"/> class.
    /// Protected constructor for derived classes to set their type identity.
    /// </summary>
    /// <param name="implementation">The schedule kind (e.g., "Interval", "Cron").</param>
    public ScheduleConfiguration(string? implementation)
    {
        Implementation = implementation;
    }























}
