using Fdw.Collections.Attributes;
using Fdw.TUI.Management.Screens;

namespace Fdw.TUI.Management.Navigation;

/// <summary>
/// Menu target for application settings.
/// </summary>
[TypeOption(typeof(MenuTargets), "settings", RestrictToCurrentCompilation = true)]
public sealed class SettingsMenuTarget : MenuTargetBase
{
    /// <summary>
    /// Creates the settings menu target.
    /// </summary>
    public SettingsMenuTarget() : base(
        id: 4,
        name: "settings",
        label: "Application Settings",
        group: "System",
        order: 10)
    {
    }

    /// <inheritdoc />
    public override NavigationResult Navigate(IScreenFactory screenFactory)
    {
        return NavigationResult.Push(screenFactory.Create<SettingsScreen>());
    }
}
