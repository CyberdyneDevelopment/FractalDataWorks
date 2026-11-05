using Fdw.Collections;

namespace Fdw.Services.Tags.Abstractions;

/// <summary>One kind of owner a tag can have. The name is the value a tag stores (the closed set <c>CK_Tag_OwnerKind</c> holds in the database).</summary>
public interface ITagOwnerKind : ITypeOption<int, TagOwnerKindBase>
{
}
