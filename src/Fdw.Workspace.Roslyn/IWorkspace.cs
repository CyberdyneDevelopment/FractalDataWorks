using System.Collections.Generic;
using Fdw.Results;

namespace Fdw.Workspace.Roslyn;

/// <summary>
/// Represents a generic workspace that manages state with snapshot/rollback capabilities.
/// </summary>
/// <typeparam name="T">The type of state managed by the workspace.</typeparam>
public interface IWorkspace<T>
{
    /// <summary>
    /// Gets the current state of the workspace.
    /// </summary>
    T Current { get; }

    /// <summary>
    /// Gets the baseline state for change detection. May be null if no baseline has been set.
    /// </summary>
    T? Baseline { get; }

    /// <summary>
    /// Gets the number of snapshots currently stored.
    /// </summary>
    int SnapshotCount { get; }

    /// <summary>
    /// Gets whether there are unsaved changes (current differs from baseline).
    /// </summary>
    bool HasChanges { get; }

    /// <summary>
    /// Updates the current state of the workspace.
    /// </summary>
    /// <param name="state">The new state to set as current.</param>
    void Update(T state);

    /// <summary>
    /// Sets the baseline state for change detection.
    /// </summary>
    /// <param name="state">The state to set as baseline.</param>
    void SetBaseline(T state);

    /// <summary>
    /// Creates a named snapshot of the current state.
    /// </summary>
    /// <param name="name">A short name for the snapshot.</param>
    /// <param name="description">A description of what this snapshot represents.</param>
    /// <returns>The unique identifier of the created snapshot.</returns>
    string CreateSnapshot(string name, string description);

    /// <summary>
    /// Restores the workspace to a previously saved snapshot.
    /// </summary>
    /// <param name="snapshotId">The identifier of the snapshot to restore.</param>
    /// <returns>A result containing the restored state or an error if the snapshot was not found.</returns>
    IGenericResult<T> RestoreSnapshot(string snapshotId);

    /// <summary>
    /// Lists all snapshots in the workspace.
    /// </summary>
    /// <returns>Information about all snapshots.</returns>
    IEnumerable<SnapshotInfo> ListSnapshots();

    /// <summary>
    /// Removes a snapshot from storage.
    /// </summary>
    /// <param name="snapshotId">The identifier of the snapshot to remove.</param>
    /// <returns>True if the snapshot was removed; false if it was not found.</returns>
    bool RemoveSnapshot(string snapshotId);

    /// <summary>
    /// Clears all stored snapshots.
    /// </summary>
    void ClearSnapshots();
}