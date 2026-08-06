using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Search.Commands;

/// <summary>Search object names (tables / views / procs / functions) by pattern.</summary>
[TypeOption(typeof(SqlCommands), "SearchSymbols", RestrictToCurrentCompilation = true)]
public sealed class SearchSymbolsCommand : SqlCommandBase
{
    public SearchSymbolsCommand() : base("SearchSymbols", StandardSqlCommandCategories.Search,
        "Search object names by Pattern (substring; case-insensitive). For full-text search use SearchText instead.") { }
    public string Pattern { get; set; } = string.Empty;
    public int MaxResults { get; set; } = 100;
}
