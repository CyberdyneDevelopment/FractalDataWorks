using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Settings.Configuration;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Settings.Validation;

/// <summary>States that <see cref="RoleSettingImplementationConfiguration"/> is checked by <see cref="RoleSettingConfigurationValidator"/> when it is written (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "RoleSettingImplementationConfiguration")]
public sealed class RoleSettingConfigurationValidation : ValidatedConfiguration<RoleSettingImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="RoleSettingConfigurationValidation"/> class.</summary>
    public RoleSettingConfigurationValidation()
        : base("RoleSettingImplementationConfiguration", new RoleSettingConfigurationValidator())
    {
    }
}
