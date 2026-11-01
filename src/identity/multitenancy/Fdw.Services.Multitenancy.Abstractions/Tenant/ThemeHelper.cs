using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ColorHelper;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// Helper for generating CSS variables from tenant themes.
/// </summary>
public static class ThemeHelper
{
    private static readonly char[] HslSeparator = [' '];

    /// <summary>
    /// Generates a dictionary of CSS variables for the given theme.
    /// </summary>
    public static IDictionary<string, string> ToCssVariables(this ITenantTheme theme)
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["color-primary"] = theme.PrimaryColor,
            ["color-secondary"] = theme.SecondaryColor,
            ["color-accent"] = theme.AccentColor,
            ["color-bg"] = theme.BackgroundColor,
            ["color-surface"] = theme.SurfaceColor,
            ["color-overlay"] = theme.OverlayColor,
            ["color-success"] = theme.SuccessColor,
            ["color-warning"] = theme.WarningColor,
            ["color-error"] = theme.ErrorColor,
            ["color-info"] = theme.InfoColor,
            ["text-main"] = theme.TextMainColor,
            ["text-muted"] = theme.TextMutedColor
        };
    }

    /// <summary>
    /// Generates a CSS :root block string for the given theme.
    /// </summary>
    public static string ToCssRootBlock(this ITenantTheme theme, string selector = ":root")
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{selector} {{");
        foreach (var variable in theme.ToCssVariables())
        {
            sb.AppendLine($"    --{variable.Key}: {variable.Value};");
        }
        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>
    /// Generates a dictionary of Scalar API Reference CSS variables from the given theme.
    /// Maps FDW theme properties to <c>--scalar-*</c> CSS custom properties.
    /// </summary>
    public static IDictionary<string, string> ToScalarCssVariables(this ITenantTheme theme)
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            // Text colors
            ["--scalar-color-1"] = HslToHex(theme.TextMainColor),
            ["--scalar-color-2"] = HslToHex(theme.TextMutedColor),
            ["--scalar-color-3"] = HslToHex(theme.TextMutedColor),
            ["--scalar-color-accent"] = HslToHex(theme.AccentColor),

            // Backgrounds
            ["--scalar-background-1"] = HslToHex(theme.BackgroundColor),
            ["--scalar-background-2"] = HslToHex(theme.SurfaceColor),
            ["--scalar-background-3"] = HslToHex(theme.OverlayColor),
            ["--scalar-background-accent"] = HslToHex(theme.AccentColor) + "1f",

            // Status colors
            ["--scalar-color-green"] = HslToHex(theme.SuccessColor),
            ["--scalar-color-red"] = HslToHex(theme.ErrorColor),
            ["--scalar-color-yellow"] = HslToHex(theme.WarningColor),
            ["--scalar-color-blue"] = HslToHex(theme.InfoColor),
            ["--scalar-color-orange"] = HslToHex(theme.WarningColor),

            // UI elements
            ["--scalar-border-color"] = HslToHex(theme.OverlayColor),
            ["--scalar-button-1"] = HslToHex(theme.PrimaryColor),
            ["--scalar-button-1-hover"] = HslToHex(theme.PrimaryColor),
            ["--scalar-button-1-color"] = HslToHex(theme.TextMainColor),

            // Sidebar
            ["--scalar-sidebar-background-1"] = HslToHex(theme.SurfaceColor),
            ["--scalar-sidebar-color-1"] = HslToHex(theme.TextMainColor),
            ["--scalar-sidebar-color-2"] = HslToHex(theme.TextMutedColor),
            ["--scalar-sidebar-border-color"] = HslToHex(theme.OverlayColor),
        };
    }

    /// <summary>
    /// Generates a Scalar-themed CSS block targeting <c>.dark-mode</c> and/or <c>.light-mode</c>
    /// selectors, suitable for injection via <c>AddHeadContent()</c>.
    /// </summary>
    /// <remarks>
    /// When <paramref name="darkMode"/> is <c>true</c> (default), the block targets <c>.dark-mode</c>.
    /// When <c>false</c>, it targets <c>.light-mode</c>.
    /// Pass <c>null</c> to emit both selectors with the same theme values.
    /// </remarks>
    public static string ToScalarCssBlock(this ITenantTheme theme, bool? darkMode = null)
    {
        var variables = theme.ToScalarCssVariables();

        if (darkMode.HasValue)
        {
            string selector = darkMode.Value ? ".dark-mode" : ".light-mode";
            return BuildCssBlock(selector, variables);
        }

        var sb = new StringBuilder();
        sb.Append(BuildCssBlock(".dark-mode", variables));
        sb.Append(BuildCssBlock(".light-mode", variables));
        return sb.ToString();
    }

    /// <summary>
    /// Converts an FDW HSL string (format: <c>"h s% l%"</c>) to a hex color string (e.g., <c>#1e3a5f</c>).
    /// </summary>
    internal static string HslToHex(string hslString)
    {
        if (string.IsNullOrEmpty(hslString))
        {
            return "#000000";
        }

        // FDW HSL format: "221 83% 53%" → hue=221, saturation=83, lightness=53
        string trimmed = hslString.Trim();
        string[] parts = trimmed.Split(HslSeparator, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 3)
        {
            return "#000000";
        }

        if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int hue))
        {
            return "#000000";
        }

        string satStr = parts[1].TrimEnd('%');
        if (!byte.TryParse(satStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte saturation))
        {
            return "#000000";
        }

        string litStr = parts[2].TrimEnd('%');
        if (!byte.TryParse(litStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte lightness))
        {
            return "#000000";
        }

        HEX hex = ColorConverter.HslToHex(new HSL(hue, saturation, lightness));
        return "#" + hex.ToString().TrimStart('#');
    }

    private static string BuildCssBlock(string selector, IDictionary<string, string> variables)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{selector} {{");
        foreach (var variable in variables)
        {
            sb.AppendLine($"    {variable.Key}: {variable.Value};");
        }
        sb.AppendLine("}");
        return sb.ToString();
    }
}