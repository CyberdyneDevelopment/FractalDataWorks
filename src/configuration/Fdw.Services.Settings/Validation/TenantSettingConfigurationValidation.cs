using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Settings.Configuration;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Settings.Validation;

/// <summary>States that <see cref="TenantSettingImplementationConfiguration"/> is checked by <see cref="TenantSettingConfigurationValidator"/> when it is written (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "TenantSettingImplementationConfiguration")]
public sealed class TenantSettingConfigurationValidation : ValidatedConfiguration<TenantSettingImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="TenantSettingConfigurationValidation"/> class.</summary>
    public TenantSettingConfigurationValidation()
        : base("TenantSettingImplementationConfiguration", new TenantSettingConfigurationValidator())
    {
    }
}
