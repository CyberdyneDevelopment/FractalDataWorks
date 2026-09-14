using Fdw.Collections;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>The cardinality of a declared relationship between two of a dataverse's data sets.</summary>
public interface IDataverseRelationshipCardinality : ITypeOption<int, DataverseRelationshipCardinalityBase>
{
}
