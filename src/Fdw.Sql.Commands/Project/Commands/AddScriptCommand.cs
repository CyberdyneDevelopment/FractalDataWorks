using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Project.Commands;

/// <summary>Add a new .sql script to the workspace (in-memory; commit with ApplyWorkspaceChanges).</summary>
[TypeOption(typeof(SqlCommands), "AddScript", RestrictToCurrentCompilation = true)]
public sealed class AddScriptCommand : SqlCommandBase
{
    public AddScriptCommand()
        : base("AddScript", StandardSqlCommandCategories.Project,
               "Add a new .sql script to the workspace. Lives in memory until ApplyWorkspaceChanges writes it to disk.")
    {
    }

    public string FilePath { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
