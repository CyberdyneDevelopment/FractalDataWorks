using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Search.Commands;

/// <summary>
/// Command to find all implementations of an interface or abstract member.
/// </summary>
[TypeOption(typeof(RoslynCommands), "FindImplementations")]
public sealed class FindImplementationsCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FindImplementationsCommand"/> class.
    /// </summary>
    public FindImplementationsCommand()
        : base("FindImplementations", RoslynCommandCategories.Search, "Find every concrete type or override of a symbol located by FilePath + Position. Use when you have a symbol at a specific source location and need its implementations — e.g. before deleting an interface or abstract method, to confirm the impact. Returns a list of ImplementationInfo entries with file/line for each. For pattern-based discovery without a known location, use SearchSymbols; for typed family analysis, use FindFamilyImplementations.")
    {
    }

    /// <summary>
    /// Gets or sets the source file path.
    /// </summary>
    [System.ComponentModel.Description("Absolute path to the source document containing the interface or abstract member.")]
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the cursor position (character offset).
    /// </summary>
    [System.ComponentModel.Description("Character offset of the symbol within FilePath.")]
    public int Position { get; init; }
}
