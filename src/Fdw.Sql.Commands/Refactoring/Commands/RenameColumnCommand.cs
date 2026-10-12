using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Refactoring.Commands;

/// <summary>Rename a column on TableName from OldName to NewName, cascading every reference.</summary>
[TypeOption(typeof(SqlCommands), "RenameColumn", RestrictToCurrentCompilation = true)]
public sealed class RenameColumnCommand : SqlCommandBase
{
    public RenameColumnCommand() : base("RenameColumn", StandardSqlCommandCategories.Refactoring,
        "Rename a column. Updates the CREATE TABLE script, every view/proc/function that references it, plus any indexes / FKs that name the column.") { }
    public string TableName { get; set; } = string.Empty;
    public string? Schema { get; set; }
    public string OldName { get; set; } = string.Empty;
    public string NewName { get; set; } = string.Empty;
}
