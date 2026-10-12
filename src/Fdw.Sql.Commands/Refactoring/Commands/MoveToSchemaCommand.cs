using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Refactoring.Commands;

[TypeOption(typeof(SqlCommands), "MoveToSchema", RestrictToCurrentCompilation = true)]
public sealed class MoveToSchemaCommand : SqlCommandBase
{
    public MoveToSchemaCommand() : base("MoveToSchema", StandardSqlCommandCategories.Refactoring,
        "Move an object from one schema to another. Rewrites the CREATE statement and every reference (dbo.Foo → audit.Foo).") { }
    public string ObjectName { get; set; } = string.Empty;
    public string FromSchema { get; set; } = "dbo";
    public string ToSchema { get; set; } = string.Empty;
}
