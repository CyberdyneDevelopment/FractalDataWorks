using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.UI.Components.Icons;

/// <summary>
/// Base class for icon glyphs. Carries the path data and the svg attributes the glyph is drawn with;
/// size and colour are the call site's business and arrive on the <see cref="Icon"/> component instead.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class IconGlyphBase : TypeOptionBase<int, IconGlyphBase>, IIconGlyph
{
    /// <summary>
    /// Initializes a new instance of <see cref="IconGlyphBase"/>.
    /// </summary>
    protected IconGlyphBase(
        int id,
        string name,
        string viewBox,
        string fill,
        string stroke,
        string strokeWidth,
        bool rounded,
        string[] paths)
        : base(id, name)
    {
        ViewBox = viewBox;
        Fill = fill;
        Stroke = stroke;
        StrokeWidth = strokeWidth;
        Rounded = rounded;
        Paths = paths;
    }

    /// <inheritdoc/>
    public string ViewBox { get; }

    /// <inheritdoc/>
    public string Fill { get; }

    /// <inheritdoc/>
    public string Stroke { get; }

    /// <inheritdoc/>
    public string StrokeWidth { get; }

    /// <inheritdoc/>
    public bool Rounded { get; }

    /// <inheritdoc/>
    public IReadOnlyList<string> Paths { get; }
}
