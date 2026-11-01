using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Refactoring.Commands;

[TypeOption(typeof(SqlCommands), "RenameTable", RestrictToCurrentCompilation = true)]
public sealed class RenameTableCommand : SqlCommandBase
{
    public RenameTableCommand() : base("RenameTable", StandardSqlCommandCategories.Refactoring,
        "Rename a table. Updates CREATE TABLE + every reference across views, procs, functions, FKs, and indexes.") { }
    public string OldName { get; set; } = string.Empty;
    public string NewName { get; set; } = string.Empty;
    public string? Schema { get; set; }
}
