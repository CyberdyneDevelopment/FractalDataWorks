using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>Owns the project. Can change anything, including who else is in it.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataverseMemberRoles), "Owner")]
public sealed class OwnerDataverseMemberRoleOption : DataverseMemberRoleBase
{
    /// <summary>Initializes a new instance of the <see cref="OwnerDataverseMemberRoleOption"/> class.</summary>
    public OwnerDataverseMemberRoleOption() : base("Owner", mayWrite: true)
    {
    }
}
