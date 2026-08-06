using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Workspace.Commands;

/// <summary>Mark the current workspace state as the comparison baseline.</summary>
[TypeOption(typeof(SqlCommands), "SetBaseline", RestrictToCurrentCompilation = true)]
public sealed class SetBaselineCommand : SqlCommandBase
{
    public SetBaselineCommand()
        : base("SetBaseline", StandardSqlCommandCategories.Workspace,
               "Mark the current workspace state as the comparison baseline. CompareToBaseline reports the cumulative diff from this point.") { }
}
