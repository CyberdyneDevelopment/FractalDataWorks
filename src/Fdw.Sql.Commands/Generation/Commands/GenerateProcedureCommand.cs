using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Generation.Commands;

[TypeOption(typeof(SqlCommands), "GenerateProcedure", RestrictToCurrentCompilation = true)]
public sealed class GenerateProcedureCommand : SqlCommandBase
{
    public GenerateProcedureCommand() : base("GenerateProcedure", StandardSqlCommandCategories.Generation,
        "Generate a new CREATE PROCEDURE script. Parameters is comma-separated '@p TYPE [= default]'.") { }
    public string ProcedureName { get; set; } = string.Empty;
    public string Schema { get; set; } = "dbo";
    public string Parameters { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IncludeSetNocount { get; set; } = true;
}
