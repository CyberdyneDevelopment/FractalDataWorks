using System;

namespace Fdw.Workspace.Roslyn;

/// <summary>
/// Represents information about a managed workspace.
/// </summary>
/// <param name="Id">Unique identifier for the workspace.</param>
/// <param name="SolutionPath">Path to the solution file.</param>
/// <param name="ProjectCount">Number of projects in the solution.</param>
/// <param name="IsActive">Whether the workspace is currently active (not sleeping).</param>
/// <param name="LastAccessedAt">When the workspace was last accessed.</param>
/// <param name="LoadedAt">When the workspace was loaded.</param>
public readonly record struct ManagedWorkspaceInfo(
    string Id,
    string SolutionPath,
    int ProjectCount,
    bool IsActive,
    DateTime LastAccessedAt,
    DateTime LoadedAt);