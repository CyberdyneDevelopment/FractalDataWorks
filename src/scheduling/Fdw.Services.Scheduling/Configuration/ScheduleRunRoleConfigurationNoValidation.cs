using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Scheduling.Abstractions.Configuration;

/// <summary>States that <see cref="ScheduleRunRoleConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "ScheduleRunRoleConfiguration")]
public sealed class ScheduleRunRoleConfigurationNoValidation : UnvalidatedConfiguration<ScheduleRunRoleConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="ScheduleRunRoleConfigurationNoValidation"/> class.</summary>
    public ScheduleRunRoleConfigurationNoValidation()
        : base("ScheduleRunRoleConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
