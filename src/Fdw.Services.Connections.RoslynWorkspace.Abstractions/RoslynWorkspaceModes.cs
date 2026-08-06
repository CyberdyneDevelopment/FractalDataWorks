using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.RoslynWorkspace.Abstractions;

/// <summary>
/// TypeCollection for RoslynWorkspace operating modes.
/// </summary>
[TypeCollection(typeof(RoslynWorkspaceModeBase), typeof(IRoslynWorkspaceMode), typeof(RoslynWorkspaceModes))]
[ExcludeFromCodeCoverage]
public abstract partial class RoslynWorkspaceModes : TypeCollectionBase<RoslynWorkspaceModeBase, IRoslynWorkspaceMode>
{
}
