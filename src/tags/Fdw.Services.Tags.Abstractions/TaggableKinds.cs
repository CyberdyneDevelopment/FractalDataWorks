using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Tags.Abstractions;

/// <summary>The kinds of thing that can carry a tag.</summary>
/// <remarks>
/// Options attach themselves: this package declares the kinds the platform ships, and a package that owns another taggable thing declares its own
/// option against this collection. Nothing dispatches on a kind's name with a switch; behaviour is read off the option.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(TaggableKindBase), typeof(ITaggableKind), typeof(TaggableKinds))]
public abstract partial class TaggableKinds : TypeCollectionBase<TaggableKindBase, ITaggableKind>
{
}
