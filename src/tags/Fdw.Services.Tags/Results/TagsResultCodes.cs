using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Tags.Results;

/// <summary>Result codes raised by the tags domain.</summary>
[TypeCollection(typeof(TagsResultCodeBase), typeof(IResultCode), typeof(TagsResultCodes))]
public abstract partial class TagsResultCodes : TypeCollectionBase<TagsResultCodeBase, IResultCode>
{
}
