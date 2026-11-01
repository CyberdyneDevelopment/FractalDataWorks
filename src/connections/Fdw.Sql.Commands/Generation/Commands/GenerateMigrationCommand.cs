using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Generation.Commands;

[TypeOption(typeof(SqlCommands), "GenerateMigration", RestrictToCurrentCompilation = true)]
public sealed class GenerateMigrationCommand : SqlCommandBase
{
    public GenerateMigrationCommand() : base("GenerateMigration", StandardSqlCommandCategories.Generation,
        "Generate an ALTER script that migrates from a snapshot/baseline to the current workspace state. Pair with SetBaseline + ApplyWorkspaceChanges for safe schema evolution.") { }
    public string? FromSnapshotId { get; set; }
    public bool FromBaseline { get; set; } = true;
}
