using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>The states a membership request can be in.</summary>
/// <remarks>
/// Closed, for the same reason as DataverseMemberRoles/DataverseMemberStates:
/// dataverse.DataverseMembershipRequest.Status has carried a CHECK constraint over exactly these
/// five names since the table shipped, with no code-side validator until now — the write path
/// would have let any string through until the database refused it.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(DataverseMembershipRequestStatusBase), typeof(IDataverseMembershipRequestStatus), typeof(DataverseMembershipRequestStatuses))]
public abstract partial class DataverseMembershipRequestStatuses : TypeCollectionBase<DataverseMembershipRequestStatusBase, IDataverseMembershipRequestStatus>
{
}
