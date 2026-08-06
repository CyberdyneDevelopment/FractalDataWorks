using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Project.Commands;

/// <summary>Remove a .sql script from the workspace (in-memory; commit with ApplyWorkspaceChanges).</summary>
[TypeOption(typeof(SqlCommands), "RemoveScript", RestrictToCurrentCompilation = true)]
public sealed class RemoveScriptCommand : SqlCommandBase
{
    public RemoveScriptCommand()
        : base("RemoveScript", StandardSqlCommandCategories.Project,
               "Remove a .sql script from the workspace. In-memory removal only; ApplyWorkspaceChanges does NOT delete the file on disk.")
    {
    }

    public string FilePath { get; set; } = string.Empty;
}
