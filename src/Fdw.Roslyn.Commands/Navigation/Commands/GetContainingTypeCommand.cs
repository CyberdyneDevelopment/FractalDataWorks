using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Navigation.Commands;

/// <summary>
/// Command to get the containing type at a given position.
/// </summary>
[TypeOption(typeof(RoslynCommands), "GetContainingType")]
public sealed class GetContainingTypeCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetContainingTypeCommand"/> class.
    /// </summary>
    public GetContainingTypeCommand()
        : base("GetContainingType", RoslynCommandCategories.Navigation, "Get the type that directly contains the symbol at FilePath + Line + Column. IncludeNested=true walks up through nested types. Use to determine the enclosing scope of a member without parsing manually. Returns a single type-info entry.")
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

    /// <summary>
    /// Gets or sets a value indicating whether to include all nested containing types.
    /// </summary>
    [System.ComponentModel.Description("When true, walk up through nested types to the outermost containing type; false (default) returns the immediate containing type.")]
    public bool IncludeNested { get; init; }
}
