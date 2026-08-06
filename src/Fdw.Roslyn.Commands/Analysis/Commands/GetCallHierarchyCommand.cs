using System;
using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Analysis.Commands;

/// <summary>
/// Command to build a call hierarchy for a method.
/// </summary>
[TypeOption(typeof(RoslynCommands), "GetCallHierarchy")]
public sealed class GetCallHierarchyCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetCallHierarchyCommand"/> class.
    /// </summary>
    public GetCallHierarchyCommand()
        : base("GetCallHierarchy", RoslynCommandCategories.Analysis, "Build a call hierarchy rooted at the symbol at FilePath + Line + Column, walking MaxDepth levels in the chosen Direction ('callers' or 'callees', default 'callers'). Use to understand who triggers a method or what a method ultimately calls. Returns a nested CallHierarchyNode tree.")
    {
    }

    /// <summary>
    /// Gets or sets the source file path.
    /// </summary>
    [System.ComponentModel.Description("Absolute path to the source document containing the target method.")]
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the line number (1-based).
    /// </summary>
    [System.ComponentModel.Description("1-based line number of the target method within FilePath.")]
    public int Line { get; init; }

    /// <summary>
    /// Gets or sets the column number (1-based).
    /// </summary>
    [System.ComponentModel.Description("1-based column number of the target method within FilePath.")]
    public int Column { get; init; }

    /// <summary>
    /// Gets or sets the direction: callers or callees.
    /// </summary>
    [System.ComponentModel.Description("'callers' (default) walks methods that invoke the target; 'callees' walks methods invoked by the target.")]
    public string Direction { get; init; } = "callers";

    /// <summary>
    /// Gets or sets the maximum depth to traverse.
    /// </summary>
    [System.ComponentModel.Description("Maximum recursion depth for the hierarchy traversal (default 3).")]
    public int MaxDepth { get; init; } = 3;
}
