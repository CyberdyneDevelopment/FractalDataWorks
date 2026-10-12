using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Navigation.Commands;

/// <summary>
/// Command to find the declaration of a symbol at a given position.
/// </summary>
[TypeOption(typeof(RoslynCommands), "FindDeclaration")]
public sealed class FindDeclarationCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FindDeclarationCommand"/> class.
    /// </summary>
    public FindDeclarationCommand()
        : base("FindDeclaration", RoslynCommandCategories.Navigation, "Locate the source declaration of the symbol at FilePath + Line + Column. Use as the precise 'go to declaration' navigation, which differs from FindDefinition for partial classes and source-generated symbols. Returns file/line/column of the declaration.")
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
