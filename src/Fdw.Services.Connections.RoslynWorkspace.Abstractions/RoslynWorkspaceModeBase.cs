using Fdw.Collections;

namespace Fdw.Services.Connections.RoslynWorkspace.Abstractions;

/// <summary>
/// Base class for RoslynWorkspace operating modes.
/// </summary>
public abstract class RoslynWorkspaceModeBase : TypeOptionBase<int, RoslynWorkspaceModeBase>, IRoslynWorkspaceMode
{
    /// <summary>
    /// Initializes a new instance of <see cref="RoslynWorkspaceModeBase"/>.
    /// </summary>
    protected RoslynWorkspaceModeBase(int id, string name) : base(id, name)
    {
    }
}
