using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>Rows on either side may match many rows on the other.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataverseRelationshipCardinalities), "ManyToMany")]
public sealed class ManyToManyDataverseRelationshipCardinalityOption : DataverseRelationshipCardinalityBase
{
    /// <summary>Initializes a new instance of the <see cref="ManyToManyDataverseRelationshipCardinalityOption"/> class.</summary>
    public ManyToManyDataverseRelationshipCardinalityOption() : base("ManyToMany")
    {
    }
}
