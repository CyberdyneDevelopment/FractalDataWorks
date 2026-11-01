using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Refactoring.Commands;

/// <summary>
/// Command to move a type to its own file.
/// </summary>
[TypeOption(typeof(RoslynCommands), "MoveToFile")]
public sealed class MoveToFileCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MoveToFileCommand"/> class.
    /// </summary>
    public MoveToFileCommand()
        : base("MoveToFile", RoslynCommandCategories.Refactoring, "Move a type declaration from a multi-type file to its own file (matching the type name). Use to enforce the one-type-per-file convention. Returns the new file path.")
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
    /// Gets or sets the target file name (optional).
    /// </summary>
    public string? TargetFileName { get; set; }
}
