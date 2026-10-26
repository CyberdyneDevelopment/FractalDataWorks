using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Quality.Configuration;

namespace Fdw.Services.Quality.Commands;

/// <summary>ConfigurationCommands TypeOption for the GlossaryTermVersion configuration domain (FDW-2331).</summary>
[TypeOption(typeof(ConfigurationCommands), "GlossaryTermVersion")]
public sealed class GlossaryTermVersionConfigurationCommand : ConfigurationCommandBase<GlossaryTermVersionImplementationConfiguration>
{
    /// <inheritdoc/>
    public GlossaryTermVersionConfigurationCommand() : base("GlossaryTermVersion") { }
}
