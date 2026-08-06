using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Quality.Configuration;

namespace Fdw.Services.Quality.Commands;

/// <summary>ConfigurationCommands TypeOption for the Environment configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "Environment")]
public sealed class EnvironmentConfigurationCommand : ConfigurationCommandBase<EnvironmentConfiguration>
{
    /// <inheritdoc/>
    public EnvironmentConfigurationCommand() : base("Environment") { }
}
