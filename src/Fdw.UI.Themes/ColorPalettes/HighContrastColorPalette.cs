using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Spectre.Console;

namespace Fdw.UI.Themes;

/// <summary>
/// High contrast color palette for accessibility.
/// </summary>
/// <remarks>
/// Uses pure black/white with bold accent colors for maximum readability.
/// Designed for users with visual impairments or low-vision conditions.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ColorPalettes), "HighContrast", RestrictToCurrentCompilation = true)]
public sealed class HighContrastColorPalette : ColorPaletteBase
{
    /// <summary>
    /// Creates the high contrast color palette.
    /// </summary>
    public HighContrastColorPalette() : base(3, "HighContrast") { }

    /// <inheritdoc />
    public override Color Primary => Color.Yellow;

    /// <inheritdoc />
    public override Color Secondary => Color.Cyan1;

    /// <inheritdoc />
    public override Color Background => Color.Black;

    /// <inheritdoc />
    public override Color Foreground => Color.White;

    /// <inheritdoc />
    public override Color Muted => Color.Grey;

    /// <inheritdoc />
    public override Color Success => Color.Lime;

    /// <inheritdoc />
    public override Color Warning => Color.Yellow;

    /// <inheritdoc />
    public override Color Error => Color.Red1;

    /// <inheritdoc />
    public override Color Info => Color.Cyan1;

    /// <inheritdoc />
    public override Color Focused => Color.Yellow;

    /// <inheritdoc />
    public override Color Selected => Color.Yellow;

    /// <inheritdoc />
    public override Color Disabled => Color.Grey50;

    /// <inheritdoc />
    public override Color Hover => Color.Grey27;

    /// <inheritdoc />
    public override Color InputBorder => Color.White;

    /// <inheritdoc />
    public override Color InputBorderFocused => Color.Yellow;

    /// <inheritdoc />
    public override Color InputBorderError => Color.Red1;

    /// <inheritdoc />
    public override Color TableHeader => Color.Yellow;

    /// <inheritdoc />
    public override Color TableRowAlternate => Color.Grey15;
}
