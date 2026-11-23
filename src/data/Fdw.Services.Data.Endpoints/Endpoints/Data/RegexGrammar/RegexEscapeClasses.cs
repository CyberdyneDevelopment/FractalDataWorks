using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The escape classes the stand-in pattern generator understands, found by the character after the backslash.</summary>
/// <remarks>An escape this collection does not hold stands for the character itself, as it does in a regular expression.</remarks>
[TypeCollection(typeof(RegexEscapeClassBase), typeof(IRegexEscapeClass), typeof(RegexEscapeClasses))]
[ExcludeFromCodeCoverage]
public abstract partial class RegexEscapeClasses : TypeCollectionBase<RegexEscapeClassBase, IRegexEscapeClass>
{
}
