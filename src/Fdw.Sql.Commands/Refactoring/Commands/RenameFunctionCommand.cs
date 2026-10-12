using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Refactoring.Commands;

[TypeOption(typeof(SqlCommands), "RenameFunction", RestrictToCurrentCompilation = true)]
public sealed class RenameFunctionCommand : SqlCommandBase
{
    public RenameFunctionCommand() : base("RenameFunction", StandardSqlCommandCategories.Refactoring,
        "Rename a UDF (scalar or TVF). Updates CREATE FUNCTION + every invocation.") { }
    public string OldName { get; set; } = string.Empty;
    public string NewName { get; set; } = string.Empty;
    public string? Schema { get; set; }
}
