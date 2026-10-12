using Fdw.UI.Abstractions.Pages;

namespace Fdw.UI.Components.Pages;

/// <summary>
/// Concrete implementation of a page action.
/// </summary>
public sealed class PageAction : IPageAction
{
    /// <inheritdoc />
    public string Id { get; set; } = "";

    /// <inheritdoc />
    public string Label { get; set; } = "";

    /// <inheritdoc />
    public string? Icon { get; set; }

    /// <inheritdoc />
    public bool IsEnabled { get; set; } = true;

    /// <inheritdoc />
    public bool IsDestructive { get; set; }

    /// <inheritdoc />
    public bool RequiresConfirmation { get; set; }

    /// <inheritdoc />
    public char? Shortcut { get; set; }

    /// <summary>
    /// Creates a standard "New" action.
    /// </summary>
    public static PageAction New(string entityType) =>
        new() { Id = "new", Label = $"New {entityType}", Icon = "+", Shortcut = 'n' };

    /// <summary>
    /// Creates a standard "Edit" action.
    /// </summary>
    public static PageAction Edit() =>
        new() { Id = "edit", Label = "Edit", Icon = "✏", Shortcut = 'e' };

    /// <summary>
    /// Creates a standard "Delete" action.
    /// </summary>
    public static PageAction Delete() =>
        new() { Id = "delete", Label = "Delete", Icon = "🗑", IsDestructive = true, RequiresConfirmation = true, Shortcut = 'd' };

    /// <summary>
    /// Creates a standard "View" action.
    /// </summary>
    public static PageAction View() =>
        new() { Id = "view", Label = "View", Icon = "👁", Shortcut = 'v' };

    /// <summary>
    /// Creates a standard "Refresh" action.
    /// </summary>
    public static PageAction Refresh() =>
        new() { Id = "refresh", Label = "Refresh", Icon = "🔄", Shortcut = 'r' };
}