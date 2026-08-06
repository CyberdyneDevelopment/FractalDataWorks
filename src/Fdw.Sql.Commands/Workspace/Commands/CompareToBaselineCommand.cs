using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Workspace.Commands;

/// <summary>Compare the current workspace state to the last set baseline.</summary>
[TypeOption(typeof(SqlCommands), "CompareToBaseline", RestrictToCurrentCompilation = true)]
public sealed class CompareToBaselineCommand : SqlCommandBase
{
    public CompareToBaselineCommand()
        : base("CompareToBaseline", StandardSqlCommandCategories.Workspace,
               "Diff the current workspace against the last SetBaseline. Returns per-script change kinds (Added / Modified / Removed).") { }
}
