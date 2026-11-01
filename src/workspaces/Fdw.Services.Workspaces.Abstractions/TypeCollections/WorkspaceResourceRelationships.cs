using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>How a workspace may relate to a resource it holds — Owns, Uses or Produces.</summary>
/// <remarks>
/// A closed set, and a TypeCollection rather than an enum for the same reason
/// <see cref="WorkspaceStatuses"/> is: FDW017, and ByName lookup is what an endpoint needs to
/// reject a bad value with a message naming the field rather than a database constraint.
///
/// The matching database CHECK constraint (CK_WorkspaceResource_Relationship) is the backstop,
/// not the definition — it refuses an out-of-set value that reaches the table by some other path.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(WorkspaceResourceRelationshipBase), typeof(IWorkspaceResourceRelationship), typeof(WorkspaceResourceRelationships))]
public abstract partial class WorkspaceResourceRelationships
    : TypeCollectionBase<WorkspaceResourceRelationshipBase, IWorkspaceResourceRelationship>
{
}
