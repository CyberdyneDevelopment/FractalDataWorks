using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Settings.Configuration;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Settings.Validation;

/// <summary>States that <see cref="ServerSettingImplementationConfiguration"/> is checked by <see cref="ServerSettingConfigurationValidator"/> when it is written (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "ServerSettingImplementationConfiguration")]
public sealed class ServerSettingConfigurationValidation : ValidatedConfiguration<ServerSettingImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="ServerSettingConfigurationValidation"/> class.</summary>
    public ServerSettingConfigurationValidation()
        : base("ServerSettingImplementationConfiguration", new ServerSettingConfigurationValidator())
    {
    }
}
