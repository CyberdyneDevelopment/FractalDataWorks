using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Refactoring.Commands;

[TypeOption(typeof(SqlCommands), "ExtractView", RestrictToCurrentCompilation = true)]
public sealed class ExtractViewCommand : SqlCommandBase
{
    public ExtractViewCommand() : base("ExtractView", StandardSqlCommandCategories.Refactoring,
        "Extract a SELECT statement at the given range into a new CREATE VIEW and replace the source with a reference to the view.") { }
    public string FilePath { get; set; } = string.Empty;
    public int StartLine { get; set; }
    public int StartColumn { get; set; }
    public int EndLine { get; set; }
    public int EndColumn { get; set; }
    public string ViewName { get; set; } = string.Empty;
    public string Schema { get; set; } = "dbo";
}
