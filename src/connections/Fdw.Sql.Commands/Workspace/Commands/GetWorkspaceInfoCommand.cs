using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Workspace.Commands;

/// <summary>Summary of the loaded workspace.</summary>
[TypeOption(typeof(SqlCommands), "GetWorkspaceInfo", RestrictToCurrentCompilation = true)]
public sealed class GetWorkspaceInfoCommand : SqlCommandBase
{
    public GetWorkspaceInfoCommand()
        : base("GetWorkspaceInfo", StandardSqlCommandCategories.Workspace,
               "Return a summary of the loaded workspace: project path, script count, last-loaded timestamp.") { }
}
