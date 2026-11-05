using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Tags.Abstractions;

/// <summary>The three things that can own a tag, listed by the type-collections route so a caller reads them instead of naming them.</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(TagOwnerKindBase), typeof(ITagOwnerKind), typeof(TagOwnerKinds))]
public abstract partial class TagOwnerKinds : TypeCollectionBase<TagOwnerKindBase, ITagOwnerKind>
{
}
