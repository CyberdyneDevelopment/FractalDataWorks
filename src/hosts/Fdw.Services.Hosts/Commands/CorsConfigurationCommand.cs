using Fdw.Configuration;
using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Hosts.Commands;

/// <summary>ConfigurationCommands TypeOption for the CORS domain records.</summary>
[TypeOption(typeof(ConfigurationCommands), "Cors")]
public sealed class CorsConfigurationCommand : ConfigurationCommandBase<DomainConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="CorsConfigurationCommand"/> class.</summary>
    public CorsConfigurationCommand() : base("Cors") { }
}
