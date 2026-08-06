using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Search.Commands;

/// <summary>
/// Command to search symbols by name pattern across the solution.
/// </summary>
[TypeOption(typeof(RoslynCommands), "SearchSymbols")]
public sealed class SearchSymbolsCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SearchSymbolsCommand"/> class.
    /// </summary>
    public SearchSymbolsCommand()
        : base("SearchSymbols", RoslynCommandCategories.Search, "Search the solution for symbols whose name matches a Pattern, returning up to MaxResults (default 100) matches. Use for discovery when you don't have a precise file location for the target; by contrast, FindUsages and FindImplementations require FilePath + Position. Returns SymbolInfoResult entries with name, kind (NamedType / Method / Property / ...), full display name, and file/line/column for each hit.")
    {
    }

    /// <summary>
    /// Gets or sets the symbol name pattern to search for.
    /// </summary>
    [System.ComponentModel.Description("Symbol name pattern. Wildcards: '*' matches any sequence, '?' matches one character. Case-sensitive.")]
    public string Pattern { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum number of results to return.
    /// </summary>
    [System.ComponentModel.Description("Upper bound on returned matches (default 100).")]
    public int MaxResults { get; init; } = 100;
}
