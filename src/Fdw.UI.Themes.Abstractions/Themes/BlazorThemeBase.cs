using System;
using System.Collections.Generic;

namespace Fdw.UI.Themes;

/// <summary>
/// Abstract base class for Blazor CSS themes.
/// Subclasses populate <see cref="_tokens"/> in their constructor;
/// <see cref="Css"/> provides null-safe lookup.
/// </summary>
public abstract class BlazorThemeBase : IBlazorTheme
{
    private readonly Dictionary<string, string> _tokens =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Creates a new Blazor theme with the given identity.</summary>
    protected BlazorThemeBase(string name, string displayName)
    {
        Name = name;
        DisplayName = displayName;
    }

    /// <summary>
    /// Registers a CSS class string for the given token name.
    /// </summary>
    protected void RegisterToken(string tokenName, string cssClasses)
    {
        _tokens[tokenName] = cssClasses;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string DisplayName { get; }

    /// <inheritdoc />
    public string Css(string tokenName)
    {
        if (tokenName is null)
        {
            return string.Empty;
        }

        return _tokens.TryGetValue(tokenName, out var value) ? value : string.Empty;
    }

    // ── Component Tokens ────────────────────────────────────────────────────

    /// <inheritdoc />
    public abstract string Card { get; }

    /// <inheritdoc />
    public abstract string CardHeader { get; }

    /// <inheritdoc />
    public abstract string CardTitle { get; }

    /// <inheritdoc />
    public abstract string Button { get; }

    /// <inheritdoc />
    public abstract string ButtonPrimary { get; }

    /// <inheritdoc />
    public abstract string ButtonDanger { get; }

    /// <inheritdoc />
    public abstract string ButtonSecondary { get; }

    /// <inheritdoc />
    public abstract string Input { get; }

    /// <inheritdoc />
    public abstract string Select { get; }

    /// <inheritdoc />
    public abstract string Table { get; }

    /// <inheritdoc />
    public abstract string TableHeader { get; }

    /// <inheritdoc />
    public abstract string TableRow { get; }

    /// <inheritdoc />
    public abstract string Badge { get; }

    // ── Layout Tokens ───────────────────────────────────────────────────────

    /// <inheritdoc />
    public abstract string Sidebar { get; }

    /// <inheritdoc />
    public abstract string SidebarItem { get; }

    /// <inheritdoc />
    public abstract string SidebarItemActive { get; }

    /// <inheritdoc />
    public abstract string SidebarGroup { get; }

    /// <inheritdoc />
    public abstract string PageHeader { get; }

    /// <inheritdoc />
    public abstract string PageContent { get; }

    // ── Color Tokens ────────────────────────────────────────────────────────

    /// <inheritdoc />
    public abstract string ColorPrimary { get; }

    /// <inheritdoc />
    public abstract string ColorSecondary { get; }

    /// <inheritdoc />
    public abstract string ColorAccent { get; }

    /// <inheritdoc />
    public abstract string ColorBackground { get; }

    /// <inheritdoc />
    public abstract string ColorSurface { get; }

    /// <inheritdoc />
    public abstract string ColorText { get; }

    /// <inheritdoc />
    public abstract string ColorTextMuted { get; }

    /// <inheritdoc />
    public abstract string ColorBorder { get; }

    /// <inheritdoc />
    public abstract string ColorSuccess { get; }

    /// <inheritdoc />
    public abstract string ColorWarning { get; }

    /// <inheritdoc />
    public abstract string ColorError { get; }
}
