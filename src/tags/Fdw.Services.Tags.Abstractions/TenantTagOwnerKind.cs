using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Tags.Abstractions;

/// <summary>A tenant owns the tag: every organization and workspace of the tenant sees it.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TagOwnerKinds), "Tenant")]
public sealed class TenantTagOwnerKind : TagOwnerKindBase
{
    /// <summary>Initializes a new instance of the <see cref="TenantTagOwnerKind"/> class.</summary>
    public TenantTagOwnerKind()
        : base("Tenant")
    {
    }
}
