using System;
using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Workspace.Commands;
/// <summary>
/// Command to create a workspace snapshot.
/// </summary>
[TypeOption(typeof(RoslynCommands), "CreateSnapshot")]
public sealed class CreateSnapshotCommand : RoslynCommandBase, ISnapshotCreatingCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateSnapshotCommand"/> class.
    /// </summary>
    public CreateSnapshotCommand()
        : base("CreateSnapshot", RoslynCommandCategories.Workspace, "Capture a named snapshot of the current workspace state, with optional SnapshotDescription. Use to mark a known-good point before a risky refactor; pair with RestoreSnapshot to roll back. Returns the snapshot ID.")
    {
    }
    /// <summary>
    /// Gets or sets the snapshot name.
    /// </summary>
    [System.ComponentModel.Description("Human-readable name to assign to the snapshot.")]
    public string SnapshotName { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the snapshot description.
    /// </summary>
    [System.ComponentModel.Description("Optional longer description of what state the snapshot captures.")]
    public string SnapshotDescription { get; init; } = string.Empty;
}
