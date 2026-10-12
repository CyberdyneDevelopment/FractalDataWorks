using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Workspace.Commands;

/// <summary>
/// Command to commit in-memory document changes accumulated by prior mutation
/// commands (Rename, ExtractMethod, etc.) to disk.
/// </summary>
[TypeOption(typeof(RoslynCommands), "ApplyWorkspaceChanges")]
public sealed class ApplyWorkspaceChangesCommand : RoslynCommandBase, IWorkspaceCommitCommand, IReasonedCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplyWorkspaceChangesCommand"/> class.
    /// </summary>
    public ApplyWorkspaceChangesCommand()
        : base("ApplyWorkspaceChanges", RoslynCommandCategories.Workspace,
            "Persist in-memory document changes accumulated by prior mutation commands (Rename, MoveNamespace, MoveTypeToProject, ExtractMethod, etc.) to disk. Use as the explicit commit step in a preview-then-commit workflow. Missing target folders are created, so a document moved into a new folder lands. Set DeleteRemovedFiles after a MoveTypeToProject: without it the source file survives next to the new one and the type is declared twice, which is a duplicate-type build break — it is off by default because deleting is irreversible, and a file changed on disk since it was loaded is reported rather than deleted. Note this writes DOCUMENTS only; project and package references are written by RepairMovedReferences. Returns the paths written and deleted.")
    {
    }

    /// <summary>
    /// Gets or sets a value indicating whether files whose documents left the solution are deleted.
    /// </summary>
    /// <remarks>
    /// Off by default: deleting is irreversible and most commands never remove a document. A
    /// cross-project move does, and without this its source file survives as a duplicate declaration.
    /// </remarks>
    public bool DeleteRemovedFiles { get; set; }

    /// <summary>
    /// Gets or sets why this change is being made, recorded verbatim into the change ledger.
    /// </summary>
    public string? Reason { get; set; }
}
