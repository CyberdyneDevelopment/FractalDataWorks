using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Navigation.Commands;

/// <summary>
/// Command to find base types and interfaces of a type.
/// </summary>
[TypeOption(typeof(RoslynCommands), "FindBaseTypes")]
public sealed class FindBaseTypesCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FindBaseTypesCommand"/> class.
    /// </summary>
    public FindBaseTypesCommand()
        : base("FindBaseTypes", RoslynCommandCategories.Navigation, "Walk the base-type chain for the type at FilePath + Line + Column, listing classes and (if IncludeInterfaces=true, default) interfaces from closest to object. Use to understand a type's inheritance ancestors before override decisions or family analysis. Returns the chain with name, kind, and source location for each.")
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
    /// Gets or sets a value indicating whether to include implemented interfaces.
    /// </summary>
    [System.ComponentModel.Description("When true (default), include implemented interfaces in the result alongside base classes; set false for classes only.")]
    public bool IncludeInterfaces { get; init; } = true;
}
