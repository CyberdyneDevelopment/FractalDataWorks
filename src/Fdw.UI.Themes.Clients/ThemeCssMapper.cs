using System;
using System.Collections.Generic;
using System.Globalization;
using Fdw.UI.Themes.Clients.Models;

namespace Fdw.UI.Themes.Clients;

/// <summary>
/// Maps a <see cref="ThemeConfiguration"/> to a dictionary of CSS custom properties
/// that drive all three web UIs (MudBlazor, Tailwind, WASM) from a single source.
/// </summary>
public static class ThemeCssMapper
{
    /// <summary>
    /// Converts a <see cref="ThemeConfiguration"/> to a dictionary of CSS variable name/value pairs.
    /// Produces <c>--fdw-*</c> variables (universal bridge) and <c>--mud-palette-*</c> variables
    /// (so MudBlazor native components update without <c>!important</c> hacks).
    /// </summary>
    /// <param name="theme">The theme configuration to convert.</param>
    /// <returns>A dictionary mapping CSS variable names to their values.</returns>
    public static IDictionary<string, string> ToDictionary(ThemeConfiguration theme)
    {
        if (theme == null) throw new ArgumentNullException(nameof(theme));

        var vars = new Dictionary<string, string>(StringComparer.Ordinal);

        // Primary
        var primaryRgb = HexToRgb(theme.PrimaryColor);
        vars["--fdw-primary"] = theme.PrimaryColor;
        vars["--fdw-primary-rgb"] = $"{primaryRgb.R},{primaryRgb.G},{primaryRgb.B}";
        vars["--fdw-primary-hover"] = AdjustBrightness(theme.PrimaryColor, 0.85f);
        vars["--mud-palette-primary"] = theme.PrimaryColor;

        // Secondary
        var secondaryRgb = HexToRgb(theme.SecondaryColor);
        vars["--fdw-secondary"] = theme.SecondaryColor;
        vars["--fdw-secondary-rgb"] = $"{secondaryRgb.R},{secondaryRgb.G},{secondaryRgb.B}";
        vars["--mud-palette-secondary"] = theme.SecondaryColor;

        // Tertiary
        vars["--fdw-tertiary"] = theme.TertiaryColor;
        vars["--mud-palette-tertiary"] = theme.TertiaryColor;

        // Background
        vars["--fdw-background"] = theme.BackgroundColor;
        vars["--mud-palette-background"] = theme.BackgroundColor;

        // Surface + derived
        vars["--fdw-surface"] = theme.SurfaceColor;
        vars["--fdw-surface-elevated"] = AdjustBrightness(theme.SurfaceColor, 1.10f);
        vars["--fdw-surface-light"] = AdjustBrightness(theme.SurfaceColor, 1.20f);
        vars["--mud-palette-surface"] = theme.SurfaceColor;

        // Error
        var errorRgb = HexToRgb(theme.ErrorColor);
        vars["--fdw-error"] = theme.ErrorColor;
        vars["--fdw-error-rgb"] = $"{errorRgb.R},{errorRgb.G},{errorRgb.B}";
        vars["--mud-palette-error"] = theme.ErrorColor;

        // Warning
        var warningRgb = HexToRgb(theme.WarningColor);
        vars["--fdw-warning"] = theme.WarningColor;
        vars["--fdw-warning-rgb"] = $"{warningRgb.R},{warningRgb.G},{warningRgb.B}";
        vars["--mud-palette-warning"] = theme.WarningColor;

        // Success
        var successRgb = HexToRgb(theme.SuccessColor);
        vars["--fdw-success"] = theme.SuccessColor;
        vars["--fdw-success-rgb"] = $"{successRgb.R},{successRgb.G},{successRgb.B}";
        vars["--mud-palette-success"] = theme.SuccessColor;

        // Info
        var infoRgb = HexToRgb(theme.InfoColor);
        vars["--fdw-info"] = theme.InfoColor;
        vars["--fdw-info-rgb"] = $"{infoRgb.R},{infoRgb.G},{infoRgb.B}";
        vars["--mud-palette-info"] = theme.InfoColor;

        // Text colors
        var textPrimaryRgb = HexToRgb(theme.TextPrimary);
        vars["--fdw-text-primary"] = theme.TextPrimary;
        vars["--fdw-text-primary-rgb"] = $"{textPrimaryRgb.R},{textPrimaryRgb.G},{textPrimaryRgb.B}";
        vars["--mud-palette-text-primary"] = theme.TextPrimary;

        vars["--fdw-text-secondary"] = theme.TextSecondary;
        vars["--mud-palette-text-secondary"] = theme.TextSecondary;

        vars["--fdw-text-muted"] = theme.TextDisabled;
        vars["--mud-palette-text-disabled"] = theme.TextDisabled;

        // Typography
        vars["--fdw-font-headers"] = theme.FontFamily;
        vars["--fdw-font-body"] = theme.FontFamilyMono;

        // Border radius
        vars["--fdw-radius"] = $"{theme.BorderRadius}px";
        vars["--fdw-radius-lg"] = $"{theme.BorderRadius * 2}px";

        // Derived: border colors from surface
        vars["--fdw-border"] = AdjustBrightness(theme.SurfaceColor, 1.25f);
        vars["--fdw-border-light"] = AdjustBrightness(theme.SurfaceColor, 1.35f);

        // Derived: glow effects
        vars["--fdw-glow-primary"] = $"0 0 10px rgba({primaryRgb.R},{primaryRgb.G},{primaryRgb.B}, 0.4)";
        vars["--fdw-glow-secondary"] = $"0 0 10px rgba({secondaryRgb.R},{secondaryRgb.G},{secondaryRgb.B}, 0.4)";

        return vars;
    }

    /// <summary>
    /// Parses a hex color string (e.g. "#FF0000" or "#F00") into its RGB components.
    /// </summary>
    /// <param name="hex">The hex color string, with or without leading '#'.</param>
    /// <returns>A tuple of (R, G, B) integer values.</returns>
    public static (int R, int G, int B) HexToRgb(string hex)
    {
        if (string.IsNullOrEmpty(hex))
            return (0, 0, 0);

        hex = hex.TrimStart('#');

        if (hex.Length == 3)
        {
            hex = string.Create(6, hex, static (span, h) =>
            {
                span[0] = h[0]; span[1] = h[0];
                span[2] = h[1]; span[3] = h[1];
                span[4] = h[2]; span[5] = h[2];
            });
        }

        if (hex.Length < 6)
            return (0, 0, 0);

        return (
            int.Parse(hex.AsSpan(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            int.Parse(hex.AsSpan(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            int.Parse(hex.AsSpan(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)
        );
    }

    /// <summary>
    /// Adjusts the brightness of a hex color by a multiplicative factor.
    /// A factor &lt; 1.0 darkens, &gt; 1.0 lightens.
    /// </summary>
    /// <param name="hex">The hex color string.</param>
    /// <param name="factor">The brightness multiplier.</param>
    /// <returns>The adjusted hex color string with leading '#'.</returns>
    public static string AdjustBrightness(string hex, float factor)
    {
        var (r, g, b) = HexToRgb(hex);

        r = Math.Min(255, Math.Max(0, (int)(r * factor)));
        g = Math.Min(255, Math.Max(0, (int)(g * factor)));
        b = Math.Min(255, Math.Max(0, (int)(b * factor)));

        return $"#{r:X2}{g:X2}{b:X2}";
    }
}
