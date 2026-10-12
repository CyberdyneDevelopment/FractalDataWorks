using Fdw.Collections;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>Where a workspace is in its lifecycle.</summary>
public interface IWorkspaceStatus : ITypeOption<int, WorkspaceStatusBase>
{
}
