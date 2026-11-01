namespace Fdw.TUI.Management.Navigation;

/// <summary>
/// Service for managing screen navigation.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Gets the current screen, or null if the stack is empty.
    /// </summary>
    IScreen? Current { get; }

    /// <summary>
    /// Gets whether there are any screens on the stack.
    /// </summary>
    bool HasScreens { get; }

    /// <summary>
    /// Gets the depth of the navigation stack.
    /// </summary>
    int Depth { get; }

    /// <summary>
    /// Pushes a screen onto the navigation stack.
    /// </summary>
    void Push(IScreen screen);

    /// <summary>
    /// Pops the current screen off the navigation stack.
    /// </summary>
    /// <returns>The popped screen, or null if the stack was empty.</returns>
    IScreen? Pop();

    /// <summary>
    /// Replaces the current screen with a new one.
    /// </summary>
    void Replace(IScreen screen);

    /// <summary>
    /// Clears all screens from the navigation stack.
    /// </summary>
    void Clear();
}
