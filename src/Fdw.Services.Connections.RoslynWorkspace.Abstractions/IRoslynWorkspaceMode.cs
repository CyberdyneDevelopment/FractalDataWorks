using Fdw.Collections;

namespace Fdw.Services.Connections.RoslynWorkspace.Abstractions;

/// <summary>
/// Marker interface for RoslynWorkspace operating modes.
/// </summary>
public interface IRoslynWorkspaceMode : ITypeOption<int, RoslynWorkspaceModeBase>
{
}
