using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Quality.Configuration;

namespace Fdw.Services.Quality.Commands;

/// <summary>ConfigurationCommands TypeOption for the GlossaryTerm configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "GlossaryTerm")]
public sealed class GlossaryTermConfigurationCommand : ConfigurationCommandBase<GlossaryTermConfiguration>
{
    /// <inheritdoc/>
    public GlossaryTermConfigurationCommand() : base("GlossaryTerm") { }
}
