using Fdw.Collections;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>
/// Defines a page mode that controls UI behavior (read-only, editable, etc.).
/// </summary>
/// <remarks>
/// This is a UI-only concept for controlling form behavior.
/// Actual data operations use DataCommands from the data layer.
/// </remarks>
public interface IPageMode : ITypeOption<int, PageModeBase>
{
    /// <summary>
    /// Gets the display label for this mode.
    /// </summary>
    string Label { get; }

    /// <summary>
    /// Gets whether fields should be editable in this mode.
    /// </summary>
    bool IsEditable { get; }

    /// <summary>
    /// Gets whether this mode represents creating a new entity.
    /// </summary>
    bool IsCreateMode { get; }

    /// <summary>
    /// Gets the icon to display for this mode.
    /// </summary>
    string Icon { get; }

    /// <summary>
    /// Gets the default page title prefix for this mode.
    /// </summary>
    /// <param name="entityDisplayName">The display name of the entity type.</param>
    /// <returns>The title prefix (e.g., "New", "Edit", or empty for view).</returns>
    string GetTitlePrefix(string entityDisplayName);
}
