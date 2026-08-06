using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Spectre.Console;

namespace Fdw.UI.Themes;

/// <summary>
/// Light color palette for terminal UIs with light backgrounds.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ColorPalettes), "Light", RestrictToCurrentCompilation = true)]
public sealed class LightColorPalette : ColorPaletteBase
{
    /// <summary>
    /// Creates the light color palette.
    /// </summary>
    public LightColorPalette() : base(2, "Light") { }

    /// <inheritdoc />
    public override Color Primary => Color.Blue;

    /// <inheritdoc />
    public override Color Secondary => Color.Grey;

    /// <inheritdoc />
    public override Color Background => Color.White;

    /// <inheritdoc />
    public override Color Foreground => Color.Black;

    /// <inheritdoc />
    public override Color Muted => Color.Grey58;

    /// <inheritdoc />
    public override Color Success => Color.Green;

    /// <inheritdoc />
    public override Color Warning => Color.Orange1;

    /// <inheritdoc />
    public override Color Error => Color.Red;

    /// <inheritdoc />
    public override Color Info => Color.Blue;

    /// <inheritdoc />
    public override Color Focused => Color.Blue;

    /// <inheritdoc />
    public override Color Selected => Color.LightSkyBlue1;

    /// <inheritdoc />
    public override Color Disabled => Color.Grey58;

    /// <inheritdoc />
    public override Color Hover => Color.Grey93;

    /// <inheritdoc />
    public override Color InputBorder => Color.Grey58;

    /// <inheritdoc />
    public override Color InputBorderFocused => Color.Blue;

    /// <inheritdoc />
    public override Color InputBorderError => Color.Red;

    /// <inheritdoc />
    public override Color TableHeader => Color.Grey89;

    /// <inheritdoc />
    public override Color TableRowAlternate => Color.Grey93;
}
