using Fdw.Collections;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>The cardinality of a declared relationship between two of a workspace's data sets.</summary>
public interface IWorkspaceRelationshipCardinality : ITypeOption<int, WorkspaceRelationshipCardinalityBase>
{
}
