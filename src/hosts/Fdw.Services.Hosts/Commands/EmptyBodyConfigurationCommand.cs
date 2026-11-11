using Fdw.Configuration;
using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Hosts.Commands;

/// <summary>ConfigurationCommands TypeOption for the EmptyBody domain records.</summary>
[TypeOption(typeof(ConfigurationCommands), "EmptyBody")]
public sealed class EmptyBodyConfigurationCommand : ConfigurationCommandBase<DomainConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="EmptyBodyConfigurationCommand"/> class.</summary>
    public EmptyBodyConfigurationCommand() : base("EmptyBody") { }
}
