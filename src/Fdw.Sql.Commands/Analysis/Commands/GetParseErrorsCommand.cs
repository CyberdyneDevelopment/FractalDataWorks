using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Analysis.Commands;

/// <summary>List ScriptDom parser errors across the workspace.</summary>
[TypeOption(typeof(SqlCommands), "GetParseErrors", RestrictToCurrentCompilation = true)]
public sealed class GetParseErrorsCommand : SqlCommandBase
{
    public GetParseErrorsCommand()
        : base("GetParseErrors", StandardSqlCommandCategories.Analysis,
               "List every script that fails ScriptDom parsing, with line/column for each error. Use as a quick health check after a refactor.") { }
}
