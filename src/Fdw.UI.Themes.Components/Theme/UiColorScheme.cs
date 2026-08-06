using System.Diagnostics.CodeAnalysis;

namespace Fdw.UI.Themes.Components.Theme;

/// <summary>
/// Color scheme for UI components.
/// </summary>
// Why: pure theme descriptor — properties + literal preset values, no logic.
[ExcludeFromCodeCoverage]
public class UiColorScheme
{
    /// <summary>
    /// Gets or sets the primary color.
    /// </summary>
    public string Primary { get; set; } = "#3b82f6";

    /// <summary>
    /// Gets or sets the secondary color.
    /// </summary>
    public string Secondary { get; set; } = "#64748b";

    /// <summary>
    /// Gets or sets the success color.
    /// </summary>
    public string Success { get; set; } = "#22c55e";

    /// <summary>
    /// Gets or sets the error/danger color.
    /// </summary>
    public string Error { get; set; } = "#ef4444";

    /// <summary>
    /// Gets or sets the warning color.
    /// </summary>
    public string Warning { get; set; } = "#eab308";

    /// <summary>
    /// Gets or sets the info color.
    /// </summary>
    public string Info { get; set; } = "#0ea5e9";

    /// <summary>
    /// Gets or sets the foreground/text color.
    /// </summary>
    public string Foreground { get; set; } = "#f8fafc";

    /// <summary>
    /// Gets or sets the background color.
    /// </summary>
    public string Background { get; set; } = "#0f172a";

    /// <summary>
    /// Gets or sets the muted/disabled color.
    /// </summary>
    public string Muted { get; set; } = "#64748b";

    /// <summary>
    /// Gets or sets the border color.
    /// </summary>
    public string Border { get; set; } = "#334155";

    /// <summary>
    /// Gets the default dark color scheme.
    /// </summary>
    public static UiColorScheme Dark => new()
    {
        Primary = "#3b82f6",
        Secondary = "#64748b",
        Success = "#22c55e",
        Error = "#ef4444",
        Warning = "#eab308",
        Info = "#0ea5e9",
        Foreground = "#f8fafc",
        Background = "#0f172a",
        Muted = "#64748b",
        Border = "#334155"
    };

    /// <summary>
    /// Gets the default light color scheme.
    /// </summary>
    public static UiColorScheme Light => new()
    {
        Primary = "#2563eb",
        Secondary = "#475569",
        Success = "#16a34a",
        Error = "#dc2626",
        Warning = "#ca8a04",
        Info = "#0284c7",
        Foreground = "#0f172a",
        Background = "#f8fafc",
        Muted = "#94a3b8",
        Border = "#e2e8f0"
    };
}