using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Build.Commands;

/// <summary>Return the TSqlModelOptions (target platform, ANSI nulls, etc.) for the loaded project.</summary>
[TypeOption(typeof(SqlCommands), "GetCompilationOptions", RestrictToCurrentCompilation = true)]
public sealed class GetCompilationOptionsCommand : SqlCommandBase
{
    public GetCompilationOptionsCommand() : base("GetCompilationOptions", StandardSqlCommandCategories.Build,
        "Return the TSqlModelOptions for the loaded project: target platform, ANSI_NULLS, QUOTED_IDENTIFIER, etc.") { }
}
