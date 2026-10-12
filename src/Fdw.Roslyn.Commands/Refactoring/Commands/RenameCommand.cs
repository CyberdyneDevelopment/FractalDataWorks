using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Refactoring.Commands;

/// <summary>
/// Command to rename a symbol across the solution.
/// </summary>
[TypeOption(typeof(RoslynCommands), "Rename")]
public sealed class RenameCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RenameCommand"/> class.
    /// </summary>
    public RenameCommand()
        : base("Rename", RoslynCommandCategories.Refactoring, "Rename a symbol and update every reference across the solution. Use as the safe replacement for find-and-replace on identifiers; respects scoping, partial classes, and explicit interface implementations. Call FindUsages first if you want to preview the blast radius. Returns the count of locations renamed.")
    {
    }

    /// <summary>
    /// Gets or sets the source file path.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the line number (1-based).
    /// </summary>
    public int Line { get; set; }

    /// <summary>
    /// Gets or sets the column number (1-based).
    /// </summary>
    public int Column { get; set; }

    /// <summary>
    /// Gets or sets the new name for the symbol.
    /// </summary>
    public string NewName { get; set; } = string.Empty;
}
