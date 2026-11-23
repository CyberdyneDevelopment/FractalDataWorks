using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The quantifiers the stand-in pattern generator understands, found by the character that opens them.</summary>
/// <remarks>A character this collection does not hold opens no quantifier: the atom before it repeats exactly once.</remarks>
[TypeCollection(typeof(RegexQuantifierBase), typeof(IRegexQuantifier), typeof(RegexQuantifiers))]
[ExcludeFromCodeCoverage]
public abstract partial class RegexQuantifiers : TypeCollectionBase<RegexQuantifierBase, IRegexQuantifier>
{
}
