using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Generation.Commands;

[TypeOption(typeof(SqlCommands), "GenerateIndex", RestrictToCurrentCompilation = true)]
public sealed class GenerateIndexCommand : SqlCommandBase
{
    public GenerateIndexCommand() : base("GenerateIndex", StandardSqlCommandCategories.Generation,
        "Generate a CREATE INDEX script. Columns is comma-separated, Includes is comma-separated covering columns.") { }
    public string TableName { get; set; } = string.Empty;
    public string Schema { get; set; } = "dbo";
    public string IndexName { get; set; } = string.Empty;
    public string Columns { get; set; } = string.Empty;
    public string Includes { get; set; } = string.Empty;
    public bool Unique { get; set; }
    public bool Clustered { get; set; }
}
