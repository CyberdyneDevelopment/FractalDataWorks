using Fdw.Collections.Attributes;

namespace Fdw.TUI.Management.Navigation;

/// <summary>
/// Menu target for exiting the application.
/// </summary>
[TypeOption(typeof(MenuTargets), "exit", RestrictToCurrentCompilation = true)]
public sealed class ExitMenuTarget : MenuTargetBase
{
    /// <summary>
    /// Creates the exit menu target.
    /// </summary>
    public ExitMenuTarget() : base(
        id: 5,
        name: "exit",
        label: "Exit",
        group: "System",
        order: 99)
    {
    }

    /// <inheritdoc />
    public override NavigationResult Navigate(IScreenFactory screenFactory)
    {
        return NavigationResult.Exit();
    }
}
