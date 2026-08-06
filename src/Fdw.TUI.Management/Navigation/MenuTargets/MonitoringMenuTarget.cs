using Fdw.Collections.Attributes;
using Fdw.TUI.Management.Screens;

namespace Fdw.TUI.Management.Navigation;

/// <summary>
/// Menu target for monitoring and logs.
/// </summary>
[TypeOption(typeof(MenuTargets), "monitoring", RestrictToCurrentCompilation = true)]
public sealed class MonitoringMenuTarget : MenuTargetBase
{
    /// <summary>
    /// Creates the monitoring menu target.
    /// </summary>
    public MonitoringMenuTarget() : base(
        id: 3,
        name: "monitoring",
        label: "Monitoring & Logs",
        group: "Main",
        order: 3,
        requiresConnection: true)
    {
    }

    /// <inheritdoc />
    public override NavigationResult Navigate(IScreenFactory screenFactory)
    {
        return NavigationResult.Push(screenFactory.Create<MonitoringMenuScreen>());
    }
}
