using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Generation.Commands;

[TypeOption(typeof(SqlCommands), "GenerateTests", RestrictToCurrentCompilation = true)]
public sealed class GenerateTestsCommand : SqlCommandBase
{
    public GenerateTestsCommand() : base("GenerateTests", StandardSqlCommandCategories.Generation,
        "Generate a tSQLt test-class skeleton for the given procedure or table. Default framework is tSQLt; FakeTable / ApplyConstraint helpers included.") { }
    public string TargetObject { get; set; } = string.Empty;
    public string? Schema { get; set; }
    public string TestFramework { get; set; } = "tSQLt";
}
