using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Generation.Commands;

[TypeOption(typeof(SqlCommands), "GenerateView", RestrictToCurrentCompilation = true)]
public sealed class GenerateViewCommand : SqlCommandBase
{
    public GenerateViewCommand() : base("GenerateView", StandardSqlCommandCategories.Generation,
        "Generate a new CREATE VIEW script. Definition is the SELECT body that follows AS.") { }
    public string ViewName { get; set; } = string.Empty;
    public string Schema { get; set; } = "dbo";
    public string Definition { get; set; } = string.Empty;
}
