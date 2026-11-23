using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>The description of an identity: what the service identifies itself as and to whom, in the words of whoever set it up (FDW-6208). The identity is the thing described, whichever mechanism its typed body belongs to, so the type has no owner. Read by whoever holds <c>identities:read</c>, the permission the identity routes ask.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DescriptionTypes), OptionName)]
public sealed class IdentityDescriptionType : DescriptionTypeBase
{
    /// <summary>The type's name, which the owning domain's provider writes and reads its descriptions under.</summary>
    public const string OptionName = "Identity";

    /// <summary>Initializes a new instance of the <see cref="IdentityDescriptionType"/> class.</summary>
    public IdentityDescriptionType()
        : base(OptionName, "identities:read", hasOwner: false)
    {
    }
}
