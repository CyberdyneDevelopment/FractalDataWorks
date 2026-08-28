namespace Fdw.Services.Connections.RoslynWorkspace.Abstractions;

/// <summary>
/// A directed dependency edge between two projects in the workspace graph.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed record WorkspaceEdge(string Source, string Target, string Kind);
