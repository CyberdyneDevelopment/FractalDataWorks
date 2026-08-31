using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Universes.Abstractions;

/// <summary>Owns the project. Can change anything, including who else is in it.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(UniverseMemberRoles), "Owner")]
public sealed class OwnerUniverseMemberRoleOption : UniverseMemberRoleBase
{
    /// <summary>Initializes a new instance of the <see cref="OwnerUniverseMemberRoleOption"/> class.</summary>
    public OwnerUniverseMemberRoleOption() : base("Owner")
    {
    }
}
