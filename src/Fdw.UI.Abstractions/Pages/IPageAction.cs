namespace Fdw.UI.Abstractions.Pages;

/// <summary>
/// Represents an action that can be performed on a page or row.
/// </summary>
public interface IPageAction
{
    /// <summary>
    /// Gets the action identifier.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the action display label.
    /// </summary>
    string Label { get; }

    /// <summary>
    /// Gets the action icon (optional).
    /// </summary>
    string? Icon { get; }

    /// <summary>
    /// Gets a value indicating whether this action is enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Gets a value indicating whether this is a destructive action (delete, etc.).
    /// </summary>
    bool IsDestructive { get; }

    /// <summary>
    /// Gets a value indicating whether confirmation is required.
    /// </summary>
    bool RequiresConfirmation { get; }

    /// <summary>
    /// Gets the keyboard shortcut (e.g., "n" for New, "d" for Delete).
    /// </summary>
    char? Shortcut { get; }
}