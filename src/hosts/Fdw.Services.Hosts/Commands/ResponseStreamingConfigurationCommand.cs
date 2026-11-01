using Fdw.Configuration;
using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Hosts.Commands;

/// <summary>ConfigurationCommands TypeOption for the ResponseStreaming domain records.</summary>
[TypeOption(typeof(ConfigurationCommands), "ResponseStreaming")]
public sealed class ResponseStreamingConfigurationCommand : ConfigurationCommandBase<DomainConfiguration>
{
    /// <inheritdoc/>
    public ResponseStreamingConfigurationCommand() : base("ResponseStreaming") { }
}
