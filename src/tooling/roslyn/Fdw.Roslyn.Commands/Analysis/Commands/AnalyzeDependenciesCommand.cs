using System;
using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Analysis.Commands;

/// <summary>
/// Command to analyze type dependencies.
/// </summary>
[TypeOption(typeof(RoslynCommands), "AnalyzeDependencies")]
public sealed class AnalyzeDependenciesCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AnalyzeDependenciesCommand"/> class.
    /// </summary>
    public AnalyzeDependenciesCommand()
        : base("AnalyzeDependencies", RoslynCommandCategories.Analysis, "Enumerate the types that the type at FilePath + Line + Column depends on. Use to map concrete dependencies before refactoring or extracting a module; set IncludeSystemTypes=true to also list System.* and other framework types. Returns a list of dependency types with their containing assemblies.")
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
    /// Gets or sets whether to include System namespace types.
    /// </summary>
    [System.ComponentModel.Description("When true, include framework types (System.*, Microsoft.*) in the dependency list; defaults to false to focus on first-party dependencies.")]
    public bool IncludeSystemTypes { get; init; }
}
