using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.UI.Components.Icons;

/// <summary>
/// Interface for a registered icon glyph: the path data of one icon plus the svg attributes it ships with.
/// </summary>
public interface IIconGlyph : ITypeOption<int, IconGlyphBase>
{
    /// <summary>Gets the svg viewBox the path data is drawn against.</summary>
    string ViewBox { get; }

    /// <summary>Gets the svg fill. <c>none</c> for a stroked outline glyph, <c>currentColor</c> for a solid one.</summary>
    string Fill { get; }

    /// <summary>Gets the svg stroke, or an empty string for a solid glyph that is not stroked.</summary>
    string Stroke { get; }

    /// <summary>Gets the stroke width the glyph ships with, or an empty string when it carries none.</summary>
    string StrokeWidth { get; }

    /// <summary>Gets a value indicating whether the glyph's paths are drawn with round caps and joins.</summary>
    bool Rounded { get; }

    /// <summary>Gets the path data, in draw order.</summary>
    IReadOnlyList<string> Paths { get; }
}
