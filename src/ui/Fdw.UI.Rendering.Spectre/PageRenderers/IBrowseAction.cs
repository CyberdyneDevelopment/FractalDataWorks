using Fdw.Collections;

namespace Fdw.UI.Rendering.Spectre.PageRenderers;

/// <summary>
/// An action the user can take on a browse page (FDW-4152). The prompt's menu entries are the options that state a
/// <see cref="MenuLabel"/>; the option's id is its position in that menu.
/// </summary>
public interface IBrowseAction : ITypeOption<int, IBrowseAction>
{
    /// <summary>
    /// Gets the text the browse prompt shows for this action, or null when the action is not a menu entry
    /// (drilling down is chosen by selecting an item).
    /// </summary>
    string? MenuLabel { get; }
}
