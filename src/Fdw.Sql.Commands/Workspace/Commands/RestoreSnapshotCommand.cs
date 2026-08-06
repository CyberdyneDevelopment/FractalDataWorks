using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Workspace.Commands;

/// <summary>Restore the workspace to the state captured in SnapshotId. Destructive.</summary>
[TypeOption(typeof(SqlCommands), "RestoreSnapshot", RestrictToCurrentCompilation = true)]
public sealed class RestoreSnapshotCommand : SqlCommandBase
{
    public RestoreSnapshotCommand()
        : base("RestoreSnapshot", StandardSqlCommandCategories.Workspace,
               "Restore the workspace to the state captured in SnapshotId. Destructive — uncommitted edits since the snapshot are lost.") { }
    public string SnapshotId { get; set; } = string.Empty;
}
