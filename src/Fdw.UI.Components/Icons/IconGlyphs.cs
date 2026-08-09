using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Icons;

/// <summary>
/// TypeCollection of the icon glyphs the UI draws from. Every glyph is defined once here and rendered
/// through <see cref="Icon"/>; no page carries path data.
/// </summary>
/// <remarks>
/// Why a TypeCollection and not a private dictionary: the icon set has to be open. A downstream skin
/// package adds a glyph by declaring another <c>[TypeOption]</c> against this collection — the
/// registration generator emits the cross-assembly RegisterMember call in the entry point — and
/// rebrands a shipped one with <c>[Replaces]</c>, neither of which a dictionary private to this
/// assembly can offer. Lookup is the same frozen-dictionary ByName every other FDW collection uses,
/// and a miss yields the NotFound sentinel rather than null.
/// </remarks>
[TypeCollection(typeof(IconGlyphBase), typeof(IIconGlyph), typeof(IconGlyphs))]
[ExcludeFromCodeCoverage]
public abstract partial class IconGlyphs : TypeCollectionBase<IconGlyphBase, IIconGlyph> { }
