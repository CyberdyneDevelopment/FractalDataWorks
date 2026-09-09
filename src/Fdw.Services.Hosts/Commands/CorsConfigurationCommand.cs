using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Hosts.Commands;

/// <summary>ConfigurationCommands TypeOption for the CORS domain records.</summary>
[TypeOption(typeof(ConfigurationCommands), "Cors")]
public sealed class CorsConfigurationCommand : ConfigurationCommandBase<CorsConfiguration>
{
    /// <inheritdoc/>
    public CorsConfigurationCommand() : base("Cors") { }
}
