using Fdw.Collections;

namespace Fdw.Services.Tags.Abstractions;

/// <summary>The base of the tag owner kinds: Tenant, Organization and Workspace.</summary>
public abstract class TagOwnerKindBase : TypeOptionBase<TagOwnerKindBase>, ITagOwnerKind
{
    /// <summary>Initializes a new instance of the <see cref="TagOwnerKindBase"/> class.</summary>
    /// <param name="name">The kind's name, exactly as a tag row stores it.</param>
    protected TagOwnerKindBase(string name)
        : base(name)
    {
    }
}
