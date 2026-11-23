using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>The description of a secret manager: which store of secrets it fronts and for whom, in the words of whoever set it up (FDW-6208). The secret manager is the thing described, whichever kind its typed body belongs to, so the type has no owner. Read by whoever holds <c>system:admin</c>, the policy every secret manager route asks. It never holds, names or hints at a secret value.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DescriptionTypes), OptionName)]
public sealed class SecretManagerDescriptionType : DescriptionTypeBase
{
    /// <summary>The type's name, which the owning domain's provider writes and reads its descriptions under.</summary>
    public const string OptionName = "SecretManager";

    /// <summary>Initializes a new instance of the <see cref="SecretManagerDescriptionType"/> class.</summary>
    public SecretManagerDescriptionType()
        : base(OptionName, "system:admin", hasOwner: false)
    {
    }
}
