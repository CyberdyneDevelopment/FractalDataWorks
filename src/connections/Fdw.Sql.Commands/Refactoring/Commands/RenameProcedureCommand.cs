using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Refactoring.Commands;

[TypeOption(typeof(SqlCommands), "RenameProcedure", RestrictToCurrentCompilation = true)]
public sealed class RenameProcedureCommand : SqlCommandBase
{
    public RenameProcedureCommand() : base("RenameProcedure", StandardSqlCommandCategories.Refactoring,
        "Rename a stored procedure. Updates the CREATE PROCEDURE script + every EXEC site.") { }
    public string OldName { get; set; } = string.Empty;
    public string NewName { get; set; } = string.Empty;
    public string? Schema { get; set; }
}
