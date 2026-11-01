using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Search.Commands;

/// <summary>
/// Command to find all references to a symbol at a given position.
/// </summary>
[TypeOption(typeof(RoslynCommands), "FindUsages")]
public sealed class FindUsagesCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FindUsagesCommand"/> class.
    /// </summary>
    public FindUsagesCommand()
        : base("FindUsages", RoslynCommandCategories.Search, "Find every reference to the symbol at FilePath + Position across the entire solution. Use to assess blast radius before renaming, deleting, or refactoring a symbol — call this before any mutating refactor. IncludeDeclaration (default true) controls whether the symbol's own declaration is included. Returns a list of UsageInfo entries with file/line/column for each reference.")
    {
    }

    /// <summary>
    /// Gets or sets the source file path.
    /// </summary>
    [System.ComponentModel.Description("Absolute path to the source document containing the target symbol.")]
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the cursor position (character offset).
    /// </summary>
    [System.ComponentModel.Description("Character offset of the target symbol within FilePath.")]
    public int Position { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to include the declaration in results.
    /// </summary>
    [System.ComponentModel.Description("When true (default), include the symbol's own declaration site in the result; set false to exclude it.")]
    public bool IncludeDeclaration { get; init; } = true;
}
