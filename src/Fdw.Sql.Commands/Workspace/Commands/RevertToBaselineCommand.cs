using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Workspace.Commands;

/// <summary>Revert the workspace to the baseline state. Destructive.</summary>
[TypeOption(typeof(SqlCommands), "RevertToBaseline", RestrictToCurrentCompilation = true)]
public sealed class RevertToBaselineCommand : SqlCommandBase
{
    public RevertToBaselineCommand()
        : base("RevertToBaseline", StandardSqlCommandCategories.Workspace,
               "Revert every in-memory script to its baseline text. Destructive — uncommitted edits since the baseline are discarded.") { }
}
