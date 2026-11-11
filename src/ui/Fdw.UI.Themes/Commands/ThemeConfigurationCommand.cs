using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.UI.Themes.Configuration;

namespace Fdw.UI.Themes.Commands;

/// <summary>ConfigurationCommands TypeOption for the Theme configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "Theme")]
public sealed class ThemeConfigurationCommand : ConfigurationCommandBase<ThemeImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="ThemeConfigurationCommand"/> class.</summary>
    public ThemeConfigurationCommand() : base("Theme") { }
}
