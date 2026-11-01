using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Navigation.Commands;

/// <summary>
/// Command to get the namespace at a given position.
/// </summary>
[TypeOption(typeof(RoslynCommands), "GetNamespace")]
public sealed class GetNamespaceCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetNamespaceCommand"/> class.
    /// </summary>
    public GetNamespaceCommand()
        : base("GetNamespace", RoslynCommandCategories.Navigation, "Get the namespace containing the symbol at FilePath + Line + Column. Use as a quick way to derive a symbol's containing namespace for refactoring decisions (e.g. before moving a file). Returns the namespace name and the full namespace chain.")
    {
    }

    /// <summary>
    /// Gets or sets the source file path.
    /// </summary>
    [System.ComponentModel.Description("Absolute path to the source document containing the target symbol.")]
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the line number (1-based).
    /// </summary>
    [System.ComponentModel.Description("1-based line number of the target symbol within FilePath.")]
    public int Line { get; init; }

    /// <summary>
    /// Gets or sets the column number (1-based).
    /// </summary>
    [System.ComponentModel.Description("1-based column number of the target symbol within FilePath.")]
    public int Column { get; init; }
}
