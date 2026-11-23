using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>A tag's description: what the tag means, shown beside its name (FDW-6208). Read by whoever holds <c>tags:read</c>, as the tag is.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DescriptionTypes), OptionName)]
public sealed class TagDescriptionType : DescriptionTypeBase
{
    /// <summary>The type's name, which the tag provider writes and reads its descriptions under.</summary>
    public const string OptionName = "Tag";

    /// <summary>Initializes a new instance of the <see cref="TagDescriptionType"/> class.</summary>
    public TagDescriptionType()
        : base(OptionName, "tags:read", hasOwner: false)
    {
    }
}
