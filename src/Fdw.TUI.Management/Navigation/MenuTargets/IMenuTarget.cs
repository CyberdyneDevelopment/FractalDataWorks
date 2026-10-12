using Fdw.Collections;

namespace Fdw.TUI.Management.Navigation;

/// <summary>
/// Defines a menu target that can produce a navigation result.
/// Used with the Dispatch pattern to replace switch statements in menu handling.
/// </summary>
public interface IMenuTarget : ITypeOption<int, MenuTargetBase>
{
    /// <summary>
    /// Gets the display label shown in the menu.
    /// </summary>
    string Label { get; }

    /// <summary>
    /// Gets the menu group this target belongs to (for visual grouping).
    /// </summary>
    string Group { get; }

    /// <summary>
    /// Gets the display order within the menu.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Gets whether this menu target is currently available.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Gets whether this target requires an active connection.
    /// </summary>
    bool RequiresConnection { get; }

    /// <summary>
    /// Creates the navigation result for this menu target.
    /// </summary>
    /// <param name="screenFactory">Factory to create screen instances.</param>
    /// <returns>The navigation result.</returns>
    NavigationResult Navigate(IScreenFactory screenFactory);
}
