using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Universes.Commands;

/// <summary>ConfigurationCommands TypeOption for the SavedView configuration domain.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "SavedView")]
public sealed class SavedViewConfigurationCommand : ConfigurationCommandBase<SavedViewConfiguration>
{
    /// <inheritdoc/>
    public SavedViewConfigurationCommand() : base("SavedView") { }
}
