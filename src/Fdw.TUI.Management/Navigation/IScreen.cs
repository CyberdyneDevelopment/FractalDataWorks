using System.Threading.Tasks;

namespace Fdw.TUI.Management.Navigation;

/// <summary>
/// Represents a screen in the TUI application.
/// </summary>
public interface IScreen
{
    /// <summary>
    /// Gets the title of the screen.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Shows the screen and returns a navigation result.
    /// </summary>
    /// <returns>The navigation result indicating what to do next.</returns>
    Task<NavigationResult> Show();
}