using Fdw.Configuration;
using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Hosts.Commands;

/// <summary>ConfigurationCommands TypeOption for the WebMcp domain records.</summary>
[TypeOption(typeof(ConfigurationCommands), "WebMcp")]
public sealed class WebMcpConfigurationCommand : ConfigurationCommandBase<DomainConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="WebMcpConfigurationCommand"/> class.</summary>
    public WebMcpConfigurationCommand() : base("WebMcp") { }
}
