using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>A permission's description: what the permission lets its holder do (FDW-6208). Read by whoever holds <c>settings/role:read</c>, the permission the permission list asks. The permission is the thing described, so the type has no owner.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DescriptionTypes), OptionName)]
public sealed class PermissionDescriptionType : DescriptionTypeBase
{
    /// <summary>The type's name, which the permission provider writes and reads its descriptions under.</summary>
    public const string OptionName = "Permission";

    /// <summary>Initializes a new instance of the <see cref="PermissionDescriptionType"/> class.</summary>
    public PermissionDescriptionType()
        : base(OptionName, "settings/role:read", hasOwner: false)
    {
    }
}
