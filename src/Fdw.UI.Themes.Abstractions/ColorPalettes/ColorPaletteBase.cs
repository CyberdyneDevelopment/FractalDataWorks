using Fdw.Collections;
using Spectre.Console;

namespace Fdw.UI.Themes;

/// <summary>
/// Abstract base class for color palettes.
/// Inherit from this class and apply [TypeOption] attribute to create custom palettes.
/// </summary>
public abstract class ColorPaletteBase : TypeOptionBase<int, ColorPaletteBase>, IColorPalette
{
    /// <summary>
    /// Creates a new color palette.
    /// </summary>
    /// <param name="id">Unique identifier.</param>
    /// <param name="name">Display name.</param>
    protected ColorPaletteBase(int id, string name) : base(id, name) { }

    /// <inheritdoc />
    public abstract Color Primary { get; }

    /// <inheritdoc />
    public abstract Color Secondary { get; }

    /// <inheritdoc />
    public abstract Color Background { get; }

    /// <inheritdoc />
    public abstract Color Foreground { get; }

    /// <inheritdoc />
    public abstract Color Muted { get; }

    /// <inheritdoc />
    public abstract Color Success { get; }

    /// <inheritdoc />
    public abstract Color Warning { get; }

    /// <inheritdoc />
    public abstract Color Error { get; }

    /// <inheritdoc />
    public abstract Color Info { get; }

    /// <inheritdoc />
    public abstract Color Focused { get; }

    /// <inheritdoc />
    public abstract Color Selected { get; }

    /// <inheritdoc />
    public abstract Color Disabled { get; }

    /// <inheritdoc />
    public abstract Color Hover { get; }

    /// <inheritdoc />
    public abstract Color InputBorder { get; }

    /// <inheritdoc />
    public abstract Color InputBorderFocused { get; }

    /// <inheritdoc />
    public abstract Color InputBorderError { get; }

    /// <inheritdoc />
    public abstract Color TableHeader { get; }

    /// <inheritdoc />
    public abstract Color TableRowAlternate { get; }
}
