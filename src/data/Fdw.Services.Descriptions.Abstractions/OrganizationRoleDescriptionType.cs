using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>An organization role's description: what the role is for inside one organization (FDW-6208). Read by whoever holds <c>organizations:read</c>, the permission the organization role list asks. The role is the thing described, so the type has no owner.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DescriptionTypes), OptionName)]
public sealed class OrganizationRoleDescriptionType : DescriptionTypeBase
{
    /// <summary>The type's name, which the organization provider writes and reads its descriptions under.</summary>
    public const string OptionName = "OrganizationRole";

    /// <summary>Initializes a new instance of the <see cref="OrganizationRoleDescriptionType"/> class.</summary>
    public OrganizationRoleDescriptionType()
        : base(OptionName, "organizations:read", hasOwner: false)
    {
    }
}
