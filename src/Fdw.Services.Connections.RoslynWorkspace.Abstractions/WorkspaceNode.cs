namespace Fdw.Services.Connections.RoslynWorkspace.Abstractions;

/// <summary>
/// A node in the workspace project-dependency graph.
/// Each node represents a project in the loaded solution.
/// </summary>
// Why: pure positional record (DTO), auto-generated properties only, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed record WorkspaceNode(string Id, string Name, string Kind);
