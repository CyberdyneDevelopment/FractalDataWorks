using System.Diagnostics.CodeAnalysis;

namespace Fdw.Workspace.Roslyn;

/// <summary>
/// Information about a snapshot without the actual state data.
/// </summary>
/// <param name="Id">The unique identifier of the snapshot.</param>
/// <param name="Name">A short, descriptive name for the snapshot.</param>
/// <param name="Description">A detailed description of what this snapshot represents.</param>
/// <param name="CreatedAt">The UTC timestamp when the snapshot was created.</param>
[ExcludeFromCodeCoverage] // Excluded: requires Roslyn MSBuildWorkspace
public sealed record SnapshotInfo(
    string Id,
    string Name,
    string Description,
    System.DateTime CreatedAt);