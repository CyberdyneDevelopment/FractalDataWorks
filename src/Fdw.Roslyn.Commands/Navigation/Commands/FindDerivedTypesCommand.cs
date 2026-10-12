using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Navigation.Commands;

/// <summary>
/// Command to find all types derived from a given type.
/// </summary>
[TypeOption(typeof(RoslynCommands), "FindDerivedTypes")]
public sealed class FindDerivedTypesCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FindDerivedTypesCommand"/> class.
    /// </summary>
    public FindDerivedTypesCommand()
        : base("FindDerivedTypes", RoslynCommandCategories.Navigation, "Find every type that derives from the class or implements the interface at FilePath + Line + Column. Transitive=true walks the full descendant tree; false (default) returns only direct derivatives. Use to scope a base-class change or to map an inheritance family. Returns a list of derived types with file/line.")
    {
    }

    /// <summary>
    /// Gets or sets the source file path.
    /// </summary>
    [System.ComponentModel.Description("Absolute path to the source document containing the target type.")]
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the line number (1-based).
    /// </summary>
    [System.ComponentModel.Description("1-based line number of the target type within FilePath.")]
    public int Line { get; init; }

    /// <summary>
    /// Gets or sets the column number (1-based).
    /// </summary>
    [System.ComponentModel.Description("1-based column number of the target type within FilePath.")]
    public int Column { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to include all derived types recursively.
    /// </summary>
    [System.ComponentModel.Description("When true, walks the full descendant tree; false (default) returns only direct derivatives.")]
    public bool Transitive { get; init; }
}
