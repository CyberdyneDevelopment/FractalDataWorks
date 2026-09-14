using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>The dataverse owns this resource — archiving the project takes it with it.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataverseResourceRelationships), "Owns")]
public sealed class OwnsDataverseResourceRelationshipOption : DataverseResourceRelationshipBase
{
    /// <summary>Initializes a new instance of the <see cref="OwnsDataverseResourceRelationshipOption"/> class.</summary>
    public OwnsDataverseResourceRelationshipOption() : base("Owns")
    {
    }
}
