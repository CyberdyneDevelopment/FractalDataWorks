using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>Each row on the left matches at most one row on the right, and vice versa.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataverseRelationshipCardinalities), "OneToOne")]
public sealed class OneToOneDataverseRelationshipCardinalityOption : DataverseRelationshipCardinalityBase
{
    /// <summary>Initializes a new instance of the <see cref="OneToOneDataverseRelationshipCardinalityOption"/> class.</summary>
    public OneToOneDataverseRelationshipCardinalityOption() : base("OneToOne")
    {
    }
}
