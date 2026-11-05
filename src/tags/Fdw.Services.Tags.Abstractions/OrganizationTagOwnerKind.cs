using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Tags.Abstractions;

/// <summary>An organization owns the tag: its workspaces see it.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TagOwnerKinds), "Organization")]
public sealed class OrganizationTagOwnerKind : TagOwnerKindBase
{
    /// <summary>Initializes a new instance of the <see cref="OrganizationTagOwnerKind"/> class.</summary>
    public OrganizationTagOwnerKind()
        : base("Organization")
    {
    }
}
