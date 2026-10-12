namespace Fdw.UI.Abstractions.Pages;

/// <summary>
/// Represents a breadcrumb item for navigation context.
/// </summary>
public interface IBreadcrumbItem
{
    /// <summary>
    /// Gets the display label.
    /// </summary>
    string Label { get; }

    /// <summary>
    /// Gets the navigation target (null for current page).
    /// </summary>
    string? NavigationTarget { get; }

    /// <summary>
    /// Gets a value indicating whether this is the current page.
    /// </summary>
    bool IsCurrent { get; }
}