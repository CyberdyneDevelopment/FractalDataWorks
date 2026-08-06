using Fdw.Collections.Attributes;
using Fdw.TUI.Management.Screens;

namespace Fdw.TUI.Management.Navigation;

/// <summary>
/// Menu target for configuration management.
/// </summary>
[TypeOption(typeof(MenuTargets), "configuration", RestrictToCurrentCompilation = true)]
public sealed class ConfigurationMenuTarget : MenuTargetBase
{
    /// <summary>
    /// Creates the configuration menu target.
    /// </summary>
    public ConfigurationMenuTarget() : base(
        id: 2,
        name: "configuration",
        label: "Configuration Management",
        group: "Main",
        order: 2,
        requiresConnection: true)
    {
    }

    /// <inheritdoc />
    public override NavigationResult Navigate(IScreenFactory screenFactory)
    {
        return NavigationResult.Push(screenFactory.Create<ConfigurationMenuScreen>());
    }
}
