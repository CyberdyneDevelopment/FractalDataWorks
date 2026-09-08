using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Universes.Abstractions;

/// <summary>The states a universe membership can be in.</summary>
/// <remarks>
/// Closed, for the same reason as UniverseMemberRoles: a state describes what the universes domain
/// does about a membership, so one contributed by another package would be a name with no
/// behaviour behind it. <c>universe.UniverseMember.State</c> has carried a CHECK constraint over
/// exactly these four names since the table shipped — this collection is the code side of that
/// constraint, which had no code side at all, so the write path validated MemberRole and let any
/// string through as a State until the database refused it.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(UniverseMemberStateBase), typeof(IUniverseMemberState), typeof(UniverseMemberStates))]
public abstract partial class UniverseMemberStates : TypeCollectionBase<UniverseMemberStateBase, IUniverseMemberState>
{
}
