using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Hosts.Commands;

/// <summary>ConfigurationCommands TypeOption for the EmptyBody domain records.</summary>
[TypeOption(typeof(ConfigurationCommands), "EmptyBody")]
public sealed class EmptyBodyConfigurationCommand : ConfigurationCommandBase<EmptyBodyConfiguration>
{
    /// <inheritdoc/>
    public EmptyBodyConfigurationCommand() : base("EmptyBody") { }
}
