using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Generation.Commands;

/// <summary>Generate a new CREATE TABLE script.</summary>
[TypeOption(typeof(SqlCommands), "GenerateTable", RestrictToCurrentCompilation = true)]
public sealed class GenerateTableCommand : SqlCommandBase
{
    public GenerateTableCommand() : base("GenerateTable", StandardSqlCommandCategories.Generation,
        "Generate a new CREATE TABLE script with the given name, schema, and column-spec. Columns is comma-separated 'name TYPE [NOT NULL]'.") { }
    public string TableName { get; set; } = string.Empty;
    public string Schema { get; set; } = "dbo";
    /// <summary>Comma-separated column specs: "Id INT NOT NULL, Name NVARCHAR(200) NOT NULL".</summary>
    public string Columns { get; set; } = string.Empty;
    public bool IncludePrimaryKey { get; set; } = true;
    public bool IncludeAuditColumns { get; set; }
}
