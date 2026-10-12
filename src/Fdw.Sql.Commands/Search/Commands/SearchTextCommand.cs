using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Search.Commands;

/// <summary>Full-text search across every .sql script in the workspace.</summary>
[TypeOption(typeof(SqlCommands), "SearchText", RestrictToCurrentCompilation = true)]
public sealed class SearchTextCommand : SqlCommandBase
{
    public SearchTextCommand() : base("SearchText", StandardSqlCommandCategories.Search,
        "Full-text search across every script. Use IsRegex for regex patterns. Returns up to MaxResults matches with file/line/column.") { }
    public string Pattern { get; set; } = string.Empty;
    public int MaxResults { get; set; } = 100;
    public bool IsRegex { get; set; }
    public bool CaseSensitive { get; set; }
}
