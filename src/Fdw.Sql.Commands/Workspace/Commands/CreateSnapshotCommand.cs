using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Workspace.Commands;

/// <summary>Capture the current workspace state as a named snapshot.</summary>
[TypeOption(typeof(SqlCommands), "CreateSnapshot", RestrictToCurrentCompilation = true)]
public sealed class CreateSnapshotCommand : SqlCommandBase
{
    public CreateSnapshotCommand()
        : base("CreateSnapshot", StandardSqlCommandCategories.Workspace,
               "Capture the current workspace state as a named snapshot. Pair with RestoreSnapshot to roll back a refactor.") { }
    public string SnapshotName { get; set; } = string.Empty;
    public string SnapshotDescription { get; set; } = string.Empty;
}
