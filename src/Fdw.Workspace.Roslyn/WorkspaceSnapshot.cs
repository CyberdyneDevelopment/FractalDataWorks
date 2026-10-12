using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Fdw.Workspace.Roslyn;

/// <summary>
/// Represents a point-in-time snapshot of a Roslyn Solution.
/// </summary>
/// <param name="Id">The unique identifier of the snapshot.</param>
/// <param name="Name">A short, descriptive name for the snapshot.</param>
/// <param name="Description">A detailed description of what this snapshot represents.</param>
/// <param name="Solution">The Solution state at the time of the snapshot.</param>
/// <param name="CreatedAt">The UTC timestamp when the snapshot was created.</param>
[ExcludeFromCodeCoverage] // Excluded: requires Roslyn MSBuildWorkspace
public sealed record WorkspaceSnapshot(
    string Id,
    string Name,
    string Description,
    Solution Solution,
    DateTime CreatedAt);
