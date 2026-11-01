using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Generation.Commands;

[TypeOption(typeof(SqlCommands), "GenerateFunction", RestrictToCurrentCompilation = true)]
public sealed class GenerateFunctionCommand : SqlCommandBase
{
    public GenerateFunctionCommand() : base("GenerateFunction", StandardSqlCommandCategories.Generation,
        "Generate a new CREATE FUNCTION script (scalar or inline-TVF). Kind = 'Scalar' or 'InlineTvf'.") { }
    public string FunctionName { get; set; } = string.Empty;
    public string Schema { get; set; } = "dbo";
    public string Parameters { get; set; } = string.Empty;
    public string ReturnType { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Kind { get; set; } = "Scalar";
}
