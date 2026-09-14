using Fdw.Collections;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>How a dataverse relates to a resource it holds.</summary>
public interface IDataverseResourceRelationship : ITypeOption<int, DataverseResourceRelationshipBase>
{
}
