using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Abstractions;

/// <summary>
/// Marks a command that restores a stored snapshot and needs it resolved before translation.
/// </summary>
/// <remarks>
/// The snapshot lives in the workspace, which the translator cannot reach, so the handler looks it up
/// by <see cref="SnapshotId"/> and stamps it on. Previously two separate reflection probes that had to
/// agree with each other; either one missing meant a silent no-op restore.
/// </remarks>
public interface ISnapshotRestoringCommand
{
    /// <summary>Gets the id of the snapshot to restore.</summary>
    string? SnapshotId { get; }

    /// <summary>Gets or sets the resolved snapshot. Set by the handler before translation.</summary>
    Solution? SnapshotSolution { get; set; }
}
