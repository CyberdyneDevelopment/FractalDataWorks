using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Search.Commands;

/// <summary>
/// Command to perform full-text search across source files.
/// </summary>
[TypeOption(typeof(RoslynCommands), "SearchText")]
public sealed class SearchTextCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SearchTextCommand"/> class.
    /// </summary>
    public SearchTextCommand()
        : base("SearchText", RoslynCommandCategories.Search, "Full-text search across source files for a Pattern, with IsRegex (default false) and CaseSensitive (default true) options. Use when you're hunting for string literals, comments, or any non-symbol content; for declared types or members use SearchSymbols instead. Returns up to MaxResults TextMatchInfo entries with file/line/column for each match.")
    {
    }

    /// <summary>
    /// Gets or sets the text or regex pattern to search for.
    /// </summary>
    [System.ComponentModel.Description("Text or regular-expression pattern to search for.")]
    public string Pattern { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether pattern is a regular expression.
    /// </summary>
    [System.ComponentModel.Description("When true, Pattern is treated as a regular expression; false (default) treats it as a literal substring.")]
    public bool IsRegex { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether search is case-sensitive.
    /// </summary>
    [System.ComponentModel.Description("When true (default), the match is case-sensitive; set false for case-insensitive matching.")]
    public bool CaseSensitive { get; init; } = true;

    /// <summary>
    /// Gets or sets the maximum number of results to return.
    /// </summary>
    [System.ComponentModel.Description("Upper bound on returned matches (default 100).")]
    public int MaxResults { get; init; } = 100;
}
