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
public partial class SchedulerConfiguration : DomainConfigurationBase, ISchedulerConfiguration
{






}
