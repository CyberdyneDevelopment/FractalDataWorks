using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Search.Commands;

/// <summary>
/// Command to detect duplicate code blocks.
/// </summary>
[TypeOption(typeof(RoslynCommands), "FindDuplicates")]
public sealed class FindDuplicatesCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FindDuplicatesCommand"/> class.
    /// </summary>
    public FindDuplicatesCommand()
        : base("FindDuplicates", RoslynCommandCategories.Search, "Detect token-equivalent code blocks across the loaded solution that meet MinLines (default 6) and MinTokens (default 25) thresholds. Use to surface refactor targets where similar logic has been pasted between methods or projects; at solution scale, raise thresholds to thin trivial matches. Returns DuplicateGroup entries keyed by a token hash, each listing every location with file/line/method-name and the block size — dedup is on the token stream, not the enclosing member.")
    {
    }

    /// <summary>
    /// Gets or sets the minimum number of lines for a duplicate block.
    /// </summary>
    [System.ComponentModel.Description("Minimum line count for a duplicate block to be reported (default 6). Raise at solution scale to thin trivial matches.")]
    public int MinLines { get; init; } = 6;

    /// <summary>
    /// Gets or sets the minimum number of tokens for a duplicate block.
    /// </summary>
    [System.ComponentModel.Description("Minimum token count for a duplicate block to be reported (default 25). Raise to filter out short ceremonial repetition.")]
    public int MinTokens { get; init; } = 25;
}
