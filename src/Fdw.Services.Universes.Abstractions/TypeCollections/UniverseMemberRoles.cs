using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Universes.Abstractions;

/// <summary>The roles a person can hold in a universe.</summary>
/// <remarks>
/// A closed set, like the lifecycle collections and unlike UniverseResourceKinds: a role describes
/// what the universes domain lets someone do, so a role contributed by another package would be a
/// name with no behaviour behind it. Enforced by the CHECK constraint on
/// universe.UniverseMember.MemberRole; this collection is what the write path validates against, so
/// a bad value is refused by name rather than as a constraint violation.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(UniverseMemberRoleBase), typeof(IUniverseMemberRole), typeof(UniverseMemberRoles))]
public abstract partial class UniverseMemberRoles : TypeCollectionBase<UniverseMemberRoleBase, IUniverseMemberRole>
{
}
