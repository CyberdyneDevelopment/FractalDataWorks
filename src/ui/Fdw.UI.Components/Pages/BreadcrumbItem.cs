using Fdw.UI.Abstractions.Pages;

namespace Fdw.UI.Components.Pages;

/// <summary>
/// Concrete implementation of a breadcrumb item.
/// </summary>
public sealed class BreadcrumbItem : IBreadcrumbItem
{
    /// <inheritdoc />
    public string Label { get; set; } = "";

    /// <inheritdoc />
    public string? NavigationTarget { get; set; }

    /// <inheritdoc />
    public bool IsCurrent { get; set; }

    /// <summary>
    /// Creates a root breadcrumb (e.g., "Home").
    /// </summary>
    public static BreadcrumbItem Root(string label = "Home", string target = "/") =>
        new() { Label = label, NavigationTarget = target };

    /// <summary>
    /// Creates a navigable breadcrumb.
    /// </summary>
    public static BreadcrumbItem Link(string label, string target) =>
        new() { Label = label, NavigationTarget = target };

    /// <summary>
    /// Creates the current page breadcrumb (non-navigable).
    /// </summary>
    public static BreadcrumbItem Current(string label) =>
        new() { Label = label, IsCurrent = true };
}