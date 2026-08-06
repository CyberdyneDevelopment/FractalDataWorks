using System.Diagnostics.CodeAnalysis;

namespace Fdw.UI.Themes;

/// <summary>
/// Cyberdyne dark Blazor theme — maps the reference-ui Tailwind classes
/// to semantic theme tokens.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CyberdyneTheme : BlazorThemeBase
{
    /// <summary>
    /// Creates the Cyberdyne dark Blazor theme.
    /// </summary>
    public CyberdyneTheme() : base("cyberdyne", "Cyberdyne Dark")
    {
        // Populate the ad-hoc token dictionary for Css() lookups
        RegisterToken("card", Card);
        RegisterToken("card-header", CardHeader);
        RegisterToken("card-title", CardTitle);
        RegisterToken("button", Button);
        RegisterToken("button-primary", ButtonPrimary);
        RegisterToken("button-danger", ButtonDanger);
        RegisterToken("button-secondary", ButtonSecondary);
        RegisterToken("input", Input);
        RegisterToken("select", Select);
        RegisterToken("table", Table);
        RegisterToken("table-header", TableHeader);
        RegisterToken("table-row", TableRow);
        RegisterToken("badge", Badge);
        RegisterToken("sidebar", Sidebar);
        RegisterToken("sidebar-item", SidebarItem);
        RegisterToken("sidebar-item-active", SidebarItemActive);
        RegisterToken("sidebar-group", SidebarGroup);
        RegisterToken("page-header", PageHeader);
        RegisterToken("page-content", PageContent);
        RegisterToken("color-primary", ColorPrimary);
        RegisterToken("color-secondary", ColorSecondary);
        RegisterToken("color-accent", ColorAccent);
        RegisterToken("color-background", ColorBackground);
        RegisterToken("color-surface", ColorSurface);
        RegisterToken("color-text", ColorText);
        RegisterToken("color-text-muted", ColorTextMuted);
        RegisterToken("color-border", ColorBorder);
        RegisterToken("color-success", ColorSuccess);
        RegisterToken("color-warning", ColorWarning);
        RegisterToken("color-error", ColorError);
    }

    // ── Component Tokens ────────────────────────────────────────────────────

    /// <inheritdoc />
    public override string Card => "bg-gray-800/50 border border-gray-700 rounded-lg";

    /// <inheritdoc />
    public override string CardHeader => "border-b border-gray-700 bg-gray-800/10 px-6 py-4";

    /// <inheritdoc />
    public override string CardTitle => "text-lg font-bold tracking-widest text-gray-200";

    /// <inheritdoc />
    public override string Button => "px-4 py-2 rounded text-sm font-medium transition-colors";

    /// <inheritdoc />
    public override string ButtonPrimary => "bg-red-600 hover:bg-red-700 text-white px-4 py-2 rounded text-sm font-medium transition-colors";

    /// <inheritdoc />
    public override string ButtonDanger => "bg-red-700 hover:bg-red-800 text-white px-4 py-2 rounded text-sm font-medium transition-colors";

    /// <inheritdoc />
    public override string ButtonSecondary => "bg-gray-700 hover:bg-gray-600 text-gray-200 px-4 py-2 rounded text-sm font-medium transition-colors";

    /// <inheritdoc />
    public override string Input => "bg-gray-800 border border-gray-600 rounded px-3 py-2 text-gray-200 focus:border-red-500 focus:outline-none";

    /// <inheritdoc />
    public override string Select => "bg-gray-800 border border-gray-600 rounded px-3 py-2 text-gray-200 focus:border-red-500 focus:outline-none";

    /// <inheritdoc />
    public override string Table => "w-full text-left text-sm";

    /// <inheritdoc />
    public override string TableHeader => "text-xs font-mono uppercase tracking-widest text-gray-500 border-b border-gray-700";

    /// <inheritdoc />
    public override string TableRow => "border-b border-gray-800 hover:bg-gray-800/30 transition-colors";

    /// <inheritdoc />
    public override string Badge => "inline-flex items-center gap-1 px-2 py-0.5 text-xs rounded border border-gray-700 bg-gray-800/50 text-gray-400";

    // ── Layout Tokens ───────────────────────────────────────────────────────

    /// <inheritdoc />
    public override string Sidebar => "w-64 border-r border-gray-700 bg-[hsl(224,71%,4%)] flex flex-col h-screen fixed left-0 top-0 z-50";

    /// <inheritdoc />
    public override string SidebarItem => "flex items-center gap-2 px-3 py-2 text-sm text-gray-400 hover:text-gray-200 hover:bg-gray-800/50 rounded transition-colors";

    /// <inheritdoc />
    public override string SidebarItemActive => "flex items-center gap-2 px-3 py-2 text-sm text-red-400 bg-red-500/10 rounded";

    /// <inheritdoc />
    public override string SidebarGroup => "mt-4 mb-1 px-2 text-xs font-mono uppercase tracking-widest text-gray-500";

    /// <inheritdoc />
    public override string PageHeader => "mb-8";

    /// <inheritdoc />
    public override string PageContent => "flex-1 ml-64 overflow-y-auto bg-[hsl(224,71%,4%)] p-8";

    // ── Color Tokens ────────────────────────────────────────────────────────

    /// <inheritdoc />
    public override string ColorPrimary => "text-red-500";

    /// <inheritdoc />
    public override string ColorSecondary => "text-gray-400";

    /// <inheritdoc />
    public override string ColorAccent => "text-red-400";

    /// <inheritdoc />
    public override string ColorBackground => "bg-[hsl(224,71%,4%)]";

    /// <inheritdoc />
    public override string ColorSurface => "bg-gray-800/50";

    /// <inheritdoc />
    public override string ColorText => "text-gray-200";

    /// <inheritdoc />
    public override string ColorTextMuted => "text-gray-500";

    /// <inheritdoc />
    public override string ColorBorder => "border-gray-700";

    /// <inheritdoc />
    public override string ColorSuccess => "text-green-500";

    /// <inheritdoc />
    public override string ColorWarning => "text-yellow-500";

    /// <inheritdoc />
    public override string ColorError => "text-red-500";
}
