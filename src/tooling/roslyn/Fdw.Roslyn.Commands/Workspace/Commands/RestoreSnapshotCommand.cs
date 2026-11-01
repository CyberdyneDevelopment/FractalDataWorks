using System.Text.Json.Serialization;
using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using Microsoft.CodeAnalysis;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Workspace.Commands;

/// <summary>
/// Command to restore a workspace snapshot.
/// </summary>
[TypeOption(typeof(RoslynCommands), "RestoreSnapshot")]
public sealed class RestoreSnapshotCommand : RoslynCommandBase, ISnapshotRestoringCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RestoreSnapshotCommand"/> class.
    /// </summary>
    public RestoreSnapshotCommand()
        : base("RestoreSnapshot", RoslynCommandCategories.Workspace, "Restore the workspace to the state captured in SnapshotId. Use to roll back changes after a refactor went wrong. Destructive — all current uncommitted edits to documents in the snapshot are lost. Returns the count of documents restored.")
    {
    }

    /// <summary>
    /// Gets or sets the snapshot ID to restore.
    /// </summary>
    [System.ComponentModel.Description("ID of the snapshot to restore (returned from CreateSnapshot).")]
    public string SnapshotId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the snapshot solution to restore.
    /// Set by the handler before translation; excluded from JSON because System.Text.Json's
    /// type analysis chokes on <see cref="Solution"/>'s transitive ref-struct properties at
    /// deserialization time.
    /// </summary>
    [JsonIgnore]
    public Solution? SnapshotSolution { get; set; }
}
