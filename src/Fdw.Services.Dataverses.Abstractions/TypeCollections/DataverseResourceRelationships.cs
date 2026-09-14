using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>How a dataverse may relate to a resource it holds — Owns, Uses or Produces.</summary>
/// <remarks>
/// A closed set, and a TypeCollection rather than an enum for the same reason
/// <see cref="DataverseStatuses"/> is: FDW017, and ByName lookup is what an endpoint needs to
/// reject a bad value with a message naming the field rather than a database constraint.
///
/// The matching database CHECK constraint (CK_DataverseResource_Relationship) is the backstop,
/// not the definition — it refuses an out-of-set value that reaches the table by some other path.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(DataverseResourceRelationshipBase), typeof(IDataverseResourceRelationship), typeof(DataverseResourceRelationships))]
public abstract partial class DataverseResourceRelationships
    : TypeCollectionBase<DataverseResourceRelationshipBase, IDataverseResourceRelationship>
{
}
