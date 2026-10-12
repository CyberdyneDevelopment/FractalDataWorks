using System.Collections.Generic;

namespace Fdw.Services.Connections.RoslynWorkspace.Abstractions;

/// <summary>
/// Represents the project-dependency graph of a Roslyn workspace.
/// Returned by <see cref="IRoslynWorkspaceClient.GetGraph"/>.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed record WorkspaceGraph(
    IReadOnlyList<WorkspaceNode> Nodes,
    IReadOnlyList<WorkspaceEdge> Edges);
