using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Hosts.Commands;

/// <summary>ConfigurationCommands TypeOption for the ResponseBuffering domain records.</summary>
[TypeOption(typeof(ConfigurationCommands), "ResponseBuffering")]
public sealed class ResponseBufferingConfigurationCommand : ConfigurationCommandBase<ResponseBufferingConfiguration>
{
    /// <inheritdoc/>
    public ResponseBufferingConfigurationCommand() : base("ResponseBuffering") { }
}
