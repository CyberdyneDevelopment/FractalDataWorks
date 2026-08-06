using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Refactoring.Commands;

/// <summary>
/// Command to extract an interface from a class.
/// </summary>
[TypeOption(typeof(RoslynCommands), "ExtractInterface")]
public sealed class ExtractInterfaceCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExtractInterfaceCommand"/> class.
    /// </summary>
    public ExtractInterfaceCommand()
        : base("ExtractInterface", RoslynCommandCategories.Refactoring, "Extract an interface from a class's public surface. Use to introduce a contract without changing the class's existing API; you choose which members to include. Returns the new interface's file path.")
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
    /// Gets or sets the name for the interface (optional).
    /// </summary>
    public string? InterfaceName { get; set; }
}
