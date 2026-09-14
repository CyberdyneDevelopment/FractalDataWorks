using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>The dataverse reads this resource but does not own it — it belongs to the platform or another project.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataverseResourceRelationships), "Uses")]
public sealed class UsesDataverseResourceRelationshipOption : DataverseResourceRelationshipBase
{
    /// <summary>Initializes a new instance of the <see cref="UsesDataverseResourceRelationshipOption"/> class.</summary>
    public UsesDataverseResourceRelationshipOption() : base("Uses")
    {
    }
}
