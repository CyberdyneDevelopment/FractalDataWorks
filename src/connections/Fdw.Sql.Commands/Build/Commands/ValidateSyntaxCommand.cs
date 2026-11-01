using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Build.Commands;

/// <summary>Validate the T-SQL syntax of a single file or inline code via ScriptDom.</summary>
[TypeOption(typeof(SqlCommands), "ValidateSyntax", RestrictToCurrentCompilation = true)]
public sealed class ValidateSyntaxCommand : SqlCommandBase
{
    public ValidateSyntaxCommand() : base("ValidateSyntax", StandardSqlCommandCategories.Build,
        "Validate T-SQL syntax for a file (FilePath) or an inline string (Code). ScriptDom parser errors only — semantic errors require BuildProject.") { }
    public string? FilePath { get; set; }
    public string? Code { get; set; }
}
