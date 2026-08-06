namespace Fdw.TUI.Management.Navigation;

/// <summary>
/// Result of showing a screen.
/// </summary>
public sealed class NavigationResult
{
    /// <summary>
    /// Gets the navigation action to perform.
    /// </summary>
    public INavigationAction Action { get; init; } = NavigationActions.Stay;

    /// <summary>
    /// Gets the next screen to navigate to (for Push/Replace actions).
    /// </summary>
    public IScreen? NextScreen { get; init; }

    /// <summary>
    /// Creates a result to push a new screen onto the stack.
    /// </summary>
    public static NavigationResult Push(IScreen screen) =>
        new() { Action = NavigationActions.Push, NextScreen = screen };

    /// <summary>
    /// Creates a result to pop the current screen off the stack.
    /// </summary>
    public static NavigationResult Pop() =>
        new() { Action = NavigationActions.Pop };

    /// <summary>
    /// Creates a result to replace the current screen.
    /// </summary>
    public static NavigationResult Replace(IScreen screen) =>
        new() { Action = NavigationActions.Replace, NextScreen = screen };

    /// <summary>
    /// Creates a result to exit the application.
    /// </summary>
    public static NavigationResult Exit() =>
        new() { Action = NavigationActions.Exit };

    /// <summary>
    /// Creates a result to stay on the current screen (re-render).
    /// </summary>
    public static NavigationResult Stay() =>
        new() { Action = NavigationActions.Stay };
}