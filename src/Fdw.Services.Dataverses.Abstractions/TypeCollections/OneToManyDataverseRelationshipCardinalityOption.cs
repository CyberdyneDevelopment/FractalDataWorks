using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>Each row on the left matches many rows on the right.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataverseRelationshipCardinalities), "OneToMany")]
public sealed class OneToManyDataverseRelationshipCardinalityOption : DataverseRelationshipCardinalityBase
{
    /// <summary>Initializes a new instance of the <see cref="OneToManyDataverseRelationshipCardinalityOption"/> class.</summary>
    public OneToManyDataverseRelationshipCardinalityOption() : base("OneToMany")
    {
    }
}
