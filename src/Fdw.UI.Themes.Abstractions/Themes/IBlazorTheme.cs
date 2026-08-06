using System.Collections.Generic;

namespace Fdw.UI.Themes;

/// <summary>
/// Defines a Blazor CSS theme providing component and layout class tokens.
/// Implementations map semantic token names to CSS class strings (typically Tailwind).
/// </summary>
public interface IBlazorTheme
{
    /// <summary>Theme identifier (e.g., "cyberdyne").</summary>
    string Name { get; }

    /// <summary>Human-readable display name (e.g., "Cyberdyne Dark").</summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets the CSS class string for the given token name.
    /// Returns <see cref="string.Empty"/> for unknown tokens.
    /// </summary>
    string Css(string tokenName);

    // ── Component Tokens ────────────────────────────────────────────────────

    /// <summary>Card container.</summary>
    string Card { get; }

    /// <summary>Card header area.</summary>
    string CardHeader { get; }

    /// <summary>Card title text.</summary>
    string CardTitle { get; }

    /// <summary>Default button.</summary>
    string Button { get; }

    /// <summary>Primary action button.</summary>
    string ButtonPrimary { get; }

    /// <summary>Danger/destructive button.</summary>
    string ButtonDanger { get; }

    /// <summary>Secondary/subtle button.</summary>
    string ButtonSecondary { get; }

    /// <summary>Text input.</summary>
    string Input { get; }

    /// <summary>Select / dropdown.</summary>
    string Select { get; }

    /// <summary>Table element.</summary>
    string Table { get; }

    /// <summary>Table header row.</summary>
    string TableHeader { get; }

    /// <summary>Table body row.</summary>
    string TableRow { get; }

    /// <summary>Badge / chip.</summary>
    string Badge { get; }

    // ── Layout Tokens ───────────────────────────────────────────────────────

    /// <summary>Sidebar container.</summary>
    string Sidebar { get; }

    /// <summary>Sidebar navigation item.</summary>
    string SidebarItem { get; }

    /// <summary>Sidebar navigation item when active.</summary>
    string SidebarItemActive { get; }

    /// <summary>Sidebar group header.</summary>
    string SidebarGroup { get; }

    /// <summary>Page header area.</summary>
    string PageHeader { get; }

    /// <summary>Page main content area.</summary>
    string PageContent { get; }

    // ── Color Tokens ────────────────────────────────────────────────────────

    /// <summary>Primary brand color class.</summary>
    string ColorPrimary { get; }

    /// <summary>Secondary brand color class.</summary>
    string ColorSecondary { get; }

    /// <summary>Accent color class.</summary>
    string ColorAccent { get; }

    /// <summary>Background color class.</summary>
    string ColorBackground { get; }

    /// <summary>Surface (elevated container) color class.</summary>
    string ColorSurface { get; }

    /// <summary>Default text color class.</summary>
    string ColorText { get; }

    /// <summary>Muted/secondary text color class.</summary>
    string ColorTextMuted { get; }

    /// <summary>Border color class.</summary>
    string ColorBorder { get; }

    /// <summary>Success color class.</summary>
    string ColorSuccess { get; }

    /// <summary>Warning color class.</summary>
    string ColorWarning { get; }

    /// <summary>Error color class.</summary>
    string ColorError { get; }
}
