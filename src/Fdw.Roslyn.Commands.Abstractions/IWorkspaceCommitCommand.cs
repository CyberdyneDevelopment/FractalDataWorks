namespace Fdw.Roslyn.Commands.Abstractions;

/// <summary>
/// Marks a command that commits pending in-memory changes to disk.
/// </summary>
/// <remarks>
/// The write itself belongs to the workspace, so the translator returns a placeholder and the handler
/// performs the commit and replaces the result outright.
/// </remarks>
public interface IWorkspaceCommitCommand
{
    /// <summary>Gets whether documents removed from the solution are deleted from disk.</summary>
    bool DeleteRemovedFiles { get; }
}
