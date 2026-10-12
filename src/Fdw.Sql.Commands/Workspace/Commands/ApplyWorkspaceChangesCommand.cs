using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Workspace.Commands;

/// <summary>Persist in-memory script edits to disk.</summary>
[TypeOption(typeof(SqlCommands), "ApplyWorkspaceChanges", RestrictToCurrentCompilation = true)]
public sealed class ApplyWorkspaceChangesCommand : SqlCommandBase
{
    public ApplyWorkspaceChangesCommand()
        : base("ApplyWorkspaceChanges", StandardSqlCommandCategories.Workspace,
               "Persist in-memory script edits accumulated by prior mutation commands to their .sql files on disk.") { }
}
