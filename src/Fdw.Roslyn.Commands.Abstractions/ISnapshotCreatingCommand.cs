namespace Fdw.Roslyn.Commands.Abstractions;

/// <summary>
/// Marks a command that creates a snapshot, which only the workspace can actually store.
/// </summary>
/// <remarks>
/// The translator mints a placeholder id because it cannot reach the workspace; the handler performs
/// the real store and reports the real id. Dispatching on this interface rather than on the string
/// "CreateSnapshot" means renaming the command cannot silently disable the store and leave callers
/// holding an id that resolves to nothing.
/// </remarks>
public interface ISnapshotCreatingCommand
{
    /// <summary>Gets the name to store the snapshot under.</summary>
    string SnapshotName { get; }

    /// <summary>Gets the snapshot's description.</summary>
    string SnapshotDescription { get; }
}
