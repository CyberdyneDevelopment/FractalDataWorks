using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Universes.Commands;

/// <summary>ConfigurationCommands TypeOption for the Universe configuration domain.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "Universe")]
public sealed class UniverseConfigurationCommand : ConfigurationCommandBase<UniverseConfiguration>
{
    /// <inheritdoc/>
    public UniverseConfigurationCommand() : base("Universe") { }
}
