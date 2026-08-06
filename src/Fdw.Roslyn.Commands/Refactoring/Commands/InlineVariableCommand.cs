using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Refactoring.Commands;

/// <summary>
/// Command to inline a local variable.
/// </summary>
[TypeOption(typeof(RoslynCommands), "InlineVariable")]
public sealed class InlineVariableCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InlineVariableCommand"/> class.
    /// </summary>
    public InlineVariableCommand()
        : base("InlineVariable", RoslynCommandCategories.Refactoring, "Inline a local variable, replacing each usage with the variable's initializer expression. Use to simplify code after a variable becomes redundant. Returns the count of inlined references.")
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
}
