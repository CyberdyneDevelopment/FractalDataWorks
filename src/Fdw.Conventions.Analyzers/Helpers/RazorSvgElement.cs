using System;

namespace Fdw.Conventions.Analyzers.Helpers;

/// <summary>
/// Distinguishes an <c>&lt;svg&gt;</c> element the markup <em>stamps</em> — a fixed glyph — from one it
/// <em>draws</em>, whose contents the component generates.
/// </summary>
/// <remarks>
/// The icon convention asks that a glyph be defined once and rendered through the shared icon component.
/// That advice presumes there is a glyph: a fixed shape, identifiable by name, the same at every site.
/// A drawing surface has none — a lineage graph or a node designer builds its geometry from data and
/// handles pointer input on the canvas itself, so no icon component could serve it and the rule would be
/// asking for something that does not exist. The two signals below are the element saying so itself.
/// </remarks>
internal static class RazorSvgElement
{
    /// <summary>
    /// Razor control-flow transitions that mean the element's contents are generated rather than written.
    /// </summary>
    private static readonly string[] GeneratingTransitions =
    [
        "@foreach",
        "@for ",
        "@for(",
        "@while",
        "@if",
        "@switch",
        "@{",
    ];

    private const string EventHandlerPrefix = "@on";
    private const string ClosingTag = "</svg";

    /// <summary>
    /// Determines whether the svg element opening at <paramref name="tagStart"/> is a drawing surface.
    /// </summary>
    /// <param name="text">The full document text.</param>
    /// <param name="tagStart">Index of the <c>&lt;</c> that opens the element.</param>
    /// <returns><see langword="true"/> when the element is drawn rather than stamped.</returns>
    internal static bool IsDrawn(string text, int tagStart)
    {
        var openingTagEnd = FindOpeningTagEnd(text, tagStart);

        // A canvas that handles pointer input is being drawn on, whatever its contents.
        if (text.IndexOf(EventHandlerPrefix, tagStart, openingTagEnd - tagStart, StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        if (openingTagEnd <= tagStart || text[openingTagEnd - 1] == '/')
            return false;

        var closing = text.IndexOf(ClosingTag, openingTagEnd, StringComparison.OrdinalIgnoreCase);
        if (closing < 0)
            closing = text.Length;

        foreach (var transition in GeneratingTransitions)
        {
            if (text.IndexOf(transition, openingTagEnd, closing - openingTagEnd, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Returns the index one past the <c>&gt;</c> that closes the opening tag, skipping quoted attribute
    /// values so a <c>&gt;</c> inside one does not end the tag early.
    /// </summary>
    private static int FindOpeningTagEnd(string text, int tagStart)
    {
        for (var i = tagStart; i < text.Length; i++)
        {
            var c = text[i];

            if (c == '"' || c == '\'')
            {
                i = SkipQuoted(text, i);
                continue;
            }

            if (c == '>')
                return i + 1;
        }

        return text.Length;
    }

    private static int SkipQuoted(string text, int quote)
    {
        for (var i = quote + 1; i < text.Length; i++)
        {
            if (text[i] == text[quote])
                return i;
        }

        return text.Length - 1;
    }
}
