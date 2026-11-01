using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Navigation.Commands;

/// <summary>
/// Command to find the definition of a symbol at a given position.
/// </summary>
[TypeOption(typeof(RoslynCommands), "FindDefinition")]
public sealed class FindDefinitionCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FindDefinitionCommand"/> class.
    /// </summary>
    public FindDefinitionCommand()
        : base("FindDefinition", RoslynCommandCategories.Navigation, "Navigate to the symbol definition (the canonical source location) for the symbol at FilePath + Line + Column. Use as the standard 'go to definition' navigation; for partial declarations, picks the primary definition. Returns file/line/column.")
    {
    }

    /// <summary>
    /// Gets or sets the source file path.
    /// </summary>
    [System.ComponentModel.Description("Absolute path to the source document containing the reference site.")]
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the line number (1-based).
    /// </summary>
    [System.ComponentModel.Description("1-based line number of the reference within FilePath.")]
    public int Line { get; init; }

    /// <summary>
    /// Gets or sets the column number (1-based).
    /// </summary>
    [System.ComponentModel.Description("1-based column number of the reference within FilePath.")]
    public int Column { get; init; }
}
