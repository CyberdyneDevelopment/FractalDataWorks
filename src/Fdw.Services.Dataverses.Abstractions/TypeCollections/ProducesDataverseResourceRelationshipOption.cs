using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>The dataverse generates this resource as an output.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataverseResourceRelationships), "Produces")]
public sealed class ProducesDataverseResourceRelationshipOption : DataverseResourceRelationshipBase
{
    /// <summary>Initializes a new instance of the <see cref="ProducesDataverseResourceRelationshipOption"/> class.</summary>
    public ProducesDataverseResourceRelationshipOption() : base("Produces")
    {
    }
}
