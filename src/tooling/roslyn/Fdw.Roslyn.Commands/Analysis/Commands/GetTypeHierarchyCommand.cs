using System;
using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Analysis.Commands;

/// <summary>
/// Command to get the full type hierarchy (base types and interfaces).
/// </summary>
[TypeOption(typeof(RoslynCommands), "GetTypeHierarchy")]
public sealed class GetTypeHierarchyCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetTypeHierarchyCommand"/> class.
    /// </summary>
    public GetTypeHierarchyCommand()
        : base("GetTypeHierarchy", RoslynCommandCategories.Analysis, "Build the complete inheritance and interface implementation graph for the type at FilePath + Line + Column. Use to map a type's place in its family before refactoring or hoisting members; set IncludeInterfaces=false to skip the implemented-interfaces side. Returns base chain, derived types, and implemented interfaces with their own hierarchies.")
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
    /// Gets or sets whether to include implemented interfaces.
    /// </summary>
    [System.ComponentModel.Description("When true (default), include implemented interfaces in the hierarchy; set false to limit to base/derived classes only.")]
    public bool IncludeInterfaces { get; init; } = true;
}
