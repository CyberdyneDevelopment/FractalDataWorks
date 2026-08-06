using Fdw.Collections.Attributes;
using Fdw.TUI.Management.Screens;

namespace Fdw.TUI.Management.Navigation;

/// <summary>
/// Menu target for connecting to an instance.
/// </summary>
[TypeOption(typeof(MenuTargets), "connect", RestrictToCurrentCompilation = true)]
public sealed class ConnectMenuTarget : MenuTargetBase
{
    /// <summary>
    /// Creates the connect menu target.
    /// </summary>
    public ConnectMenuTarget() : base(
        id: 1,
        name: "connect",
        label: "Connect to Instance",
        group: "Main",
        order: 1)
    {
    }

    /// <inheritdoc />
    public override NavigationResult Navigate(IScreenFactory screenFactory)
    {
        return NavigationResult.Push(screenFactory.Create<ConnectionsScreen>());
    }
}
