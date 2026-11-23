using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>A role's description: what the role is for (FDW-6208). Read by whoever holds <c>settings/role:read</c>, the permission the role list asks. The role is the thing described, so the type has no owner.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DescriptionTypes), OptionName)]
public sealed class RoleDescriptionType : DescriptionTypeBase
{
    /// <summary>The type's name, which the role provider writes and reads its descriptions under.</summary>
    public const string OptionName = "Role";

    /// <summary>Initializes a new instance of the <see cref="RoleDescriptionType"/> class.</summary>
    public RoleDescriptionType()
        : base(OptionName, "settings/role:read", hasOwner: false)
    {
    }
}
