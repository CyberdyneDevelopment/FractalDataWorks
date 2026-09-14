using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>The cardinality of a declared relationship — OneToOne, OneToMany or ManyToMany.</summary>
/// <remarks>
/// A closed set, and a TypeCollection rather than an enum for the same reason
/// <see cref="DataverseStatuses"/> is: FDW017, and ByName lookup is what an endpoint needs to
/// reject a bad value with a message naming the field rather than a database constraint.
///
/// The matching database CHECK constraint (CK_DataverseRelationship_Cardinality) is the backstop,
/// not the definition — it refuses an out-of-set value that reaches the table by some other path.
/// A relationship whose left and right data set are the same is still allowed: a self-referencing
/// hierarchy (a ManagerId column pointing back at the same data set) is legitimate, and nothing
/// here or in the database refuses it.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(DataverseRelationshipCardinalityBase), typeof(IDataverseRelationshipCardinality), typeof(DataverseRelationshipCardinalities))]
public abstract partial class DataverseRelationshipCardinalities
    : TypeCollectionBase<DataverseRelationshipCardinalityBase, IDataverseRelationshipCardinality>
{
}
