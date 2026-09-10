using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Hosts.Commands;

/// <summary>ConfigurationCommands TypeOption for the ForwardedHeaders domain records.</summary>
[TypeOption(typeof(ConfigurationCommands), "ForwardedHeaders")]
public sealed class ForwardedHeadersConfigurationCommand : ConfigurationCommandBase<ForwardedHeadersConfiguration>
{
    /// <inheritdoc/>
    public ForwardedHeadersConfigurationCommand() : base("ForwardedHeaders") { }
}
