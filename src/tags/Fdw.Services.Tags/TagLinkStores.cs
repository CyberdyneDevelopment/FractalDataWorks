using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Tags;

/// <summary>The link stores, one per taggable kind, found by the kind's name.</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(TagLinkStoreBase), typeof(ITagLinkStore), typeof(TagLinkStores))]
public abstract partial class TagLinkStores : TypeCollectionBase<TagLinkStoreBase, ITagLinkStore>
{
}
