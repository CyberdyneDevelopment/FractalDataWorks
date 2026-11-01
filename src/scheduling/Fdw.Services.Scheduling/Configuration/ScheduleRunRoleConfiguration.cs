using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Scheduling.Abstractions.Configuration;

/// <summary>
/// One role a schedule's runs act with (FDW-1161). Cascade child of <see cref="ScheduleImplementationConfiguration"/>
/// (sched.ScheduleRunRole): the roles its creator chose when the schedule was created or last changed.
/// </summary>
/// <remarks>
/// Why the schedule records them and the pipeline does not: the roles are a property of the run, and a scheduled run has no
/// person behind it to name them, so the schedule holds what its creator named. The creator had to hold each role in the tenant
/// they acted in; every fire runs with exactly these roles and is judged again, when it fires, for what the pipeline then reaches.
/// Why the role's name and not its id: runs and callers speak of roles by name, and a role that is renamed or removed stops
/// resolving, which refuses the fire naming it rather than running with less than was chosen.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Schedule")]
public sealed partial class ScheduleRunRoleConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets the name of the role the schedule's runs act with.</summary>
    public string RoleName { get; set; } = string.Empty;
}
