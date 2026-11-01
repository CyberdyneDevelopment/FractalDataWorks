using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Workspace.Commands;

/// <summary>Return info about the currently-set baseline.</summary>
[TypeOption(typeof(SqlCommands), "GetBaseline", RestrictToCurrentCompilation = true)]
public sealed class GetBaselineCommand : SqlCommandBase
{
    public GetBaselineCommand()
        : base("GetBaseline", StandardSqlCommandCategories.Workspace,
               "Return information about the currently-set baseline: has-baseline flag, script count.") { }
}
