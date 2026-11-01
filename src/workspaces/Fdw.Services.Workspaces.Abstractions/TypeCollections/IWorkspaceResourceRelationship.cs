using Fdw.Collections;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>How a workspace relates to a resource it holds.</summary>
public interface IWorkspaceResourceRelationship : ITypeOption<int, WorkspaceResourceRelationshipBase>
{
}
