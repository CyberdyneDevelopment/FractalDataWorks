using Fdw.Configuration;
using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Hosts.Commands;

/// <summary>ConfigurationCommands TypeOption for the ResponseBuffering domain records.</summary>
[TypeOption(typeof(ConfigurationCommands), "ResponseBuffering")]
public sealed class ResponseBufferingConfigurationCommand : ConfigurationCommandBase<DomainConfiguration>
{
    /// <inheritdoc/>
    public ResponseBufferingConfigurationCommand() : base("ResponseBuffering") { }
}
