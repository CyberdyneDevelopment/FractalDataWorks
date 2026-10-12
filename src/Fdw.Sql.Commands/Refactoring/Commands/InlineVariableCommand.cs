using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Refactoring.Commands;

[TypeOption(typeof(SqlCommands), "InlineVariable", RestrictToCurrentCompilation = true)]
public sealed class InlineVariableCommand : SqlCommandBase
{
    public InlineVariableCommand() : base("InlineVariable", StandardSqlCommandCategories.Refactoring,
        "Inline a DECLARE @var = expr by substituting every reference with the initializer expression.") { }
    public string FilePath { get; set; } = string.Empty;
    public int Line { get; set; }
    public int Column { get; set; }
}
