using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Hosts.Commands;

/// <summary>ConfigurationCommands TypeOption for the ApiReference domain records.</summary>
[TypeOption(typeof(ConfigurationCommands), "ApiReference")]
public sealed class ApiReferenceConfigurationCommand : ConfigurationCommandBase<ApiReferenceConfiguration>
{
    /// <inheritdoc/>
    public ApiReferenceConfigurationCommand() : base("ApiReference") { }
}
