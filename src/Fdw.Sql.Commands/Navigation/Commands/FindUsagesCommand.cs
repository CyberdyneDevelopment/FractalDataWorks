using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Navigation.Commands;

/// <summary>Find every usage of the identifier at FilePath + Line + Column across the workspace.</summary>
[TypeOption(typeof(SqlCommands), "FindUsages", RestrictToCurrentCompilation = true)]
public sealed class FindUsagesCommand : SqlCommandBase
{
    public FindUsagesCommand() : base("FindUsages", StandardSqlCommandCategories.Navigation,
        "Find every usage of the identifier at FilePath + Line + Column. ScriptDom-based, so resolves through synonyms and AS aliases when possible.") { }
    public string FilePath { get; set; } = string.Empty;
    public int Line { get; set; }
    public int Column { get; set; }
}
