using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Spectre.Console;

namespace Fdw.UI.Themes;

/// <summary>
/// Dark color palette - default theme for terminal UIs.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ColorPalettes), "Dark", RestrictToCurrentCompilation = true)]
public sealed class DarkColorPalette : ColorPaletteBase
{
    /// <summary>
    /// Creates the dark color palette.
    /// </summary>
    public DarkColorPalette() : base(1, "Dark") { }

    /// <inheritdoc />
    public override Color Primary => Color.DodgerBlue1;

    /// <inheritdoc />
    public override Color Secondary => Color.Grey;

    /// <inheritdoc />
    public override Color Background => Color.Grey11;

    /// <inheritdoc />
    public override Color Foreground => Color.White;

    /// <inheritdoc />
    public override Color Muted => Color.Grey58;

    /// <inheritdoc />
    public override Color Success => Color.Green;

    /// <inheritdoc />
    public override Color Warning => Color.Yellow;

    /// <inheritdoc />
    public override Color Error => Color.Red;

    /// <inheritdoc />
    public override Color Info => Color.Aqua;

    /// <inheritdoc />
    public override Color Focused => Color.DodgerBlue1;

    /// <inheritdoc />
    public override Color Selected => Color.Blue;

    /// <inheritdoc />
    public override Color Disabled => Color.Grey42;

    /// <inheritdoc />
    public override Color Hover => Color.Grey35;

    /// <inheritdoc />
    public override Color InputBorder => Color.Grey;

    /// <inheritdoc />
    public override Color InputBorderFocused => Color.DodgerBlue1;

    /// <inheritdoc />
    public override Color InputBorderError => Color.Red;

    /// <inheritdoc />
    public override Color TableHeader => Color.Grey23;

    /// <inheritdoc />
    public override Color TableRowAlternate => Color.Grey15;
}
