using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>Glossary and reference material.</summary>
[TypeOption(typeof(IconGlyphs), "BookOpen")]
[ExcludeFromCodeCoverage]
public sealed class BookOpenIcon : IconGlyphBase
{
    /// <summary>Initializes a new instance of <see cref="BookOpenIcon"/>.</summary>
    public BookOpenIcon()
        : base(
            33,
            "BookOpen",
            "0 0 24 24",
            "none",
            "currentColor",
            "2",
            true,
            [
                "M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253"
            ])
    { }
}
