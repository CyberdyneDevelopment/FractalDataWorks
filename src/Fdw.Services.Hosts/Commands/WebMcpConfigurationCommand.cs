using Fdw.Configuration;
using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Hosts.Commands;

/// <summary>ConfigurationCommands TypeOption for the WebMcp domain records.</summary>
[TypeOption(typeof(ConfigurationCommands), "WebMcp")]
public sealed class WebMcpConfigurationCommand : ConfigurationCommandBase<DomainConfiguration>
{
    /// <inheritdoc/>
    public WebMcpConfigurationCommand() : base("WebMcp") { }
}
