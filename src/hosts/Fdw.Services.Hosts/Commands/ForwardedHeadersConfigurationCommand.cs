using Fdw.Configuration;
using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Hosts.Commands;

/// <summary>ConfigurationCommands TypeOption for the ForwardedHeaders domain records.</summary>
[TypeOption(typeof(ConfigurationCommands), "ForwardedHeaders")]
public sealed class ForwardedHeadersConfigurationCommand : ConfigurationCommandBase<DomainConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="ForwardedHeadersConfigurationCommand"/> class.</summary>
    public ForwardedHeadersConfigurationCommand() : base("ForwardedHeaders") { }
}
