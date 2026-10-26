using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Settings.Configuration;

namespace Fdw.Services.Settings.Commands;

/// <summary>Builds the data commands for the HomePage domain (FDW-2325).</summary>
[TypeOption(typeof(ConfigurationCommands), "HomePage")]
public sealed class HomePageConfigurationCommand : ConfigurationCommandBase<HomePageImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="HomePageConfigurationCommand"/> class.</summary>
    public HomePageConfigurationCommand() : base("HomePage") { }
}
