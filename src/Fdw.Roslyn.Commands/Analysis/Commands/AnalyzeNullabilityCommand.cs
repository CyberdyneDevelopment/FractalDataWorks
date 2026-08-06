using System;
using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Analysis.Commands;

/// <summary>
/// Command to analyze nullable reference types usage.
/// </summary>
[TypeOption(typeof(RoslynCommands), "AnalyzeNullability")]
public sealed class AnalyzeNullabilityCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AnalyzeNullabilityCommand"/> class.
    /// </summary>
    public AnalyzeNullabilityCommand()
        : base("AnalyzeNullability", RoslynCommandCategories.Analysis, "Inspect nullable reference type annotations in a document and report flow-state inferences. Use to find places where nullability annotations may be wrong (mismatched declared vs flow nullability) or where additional null checks are needed. Returns a list of nullability findings with file/line.")
    {
    }

    /// <summary>
    /// Gets or sets the source file path.
    /// </summary>
    [System.ComponentModel.Description("Absolute path to the source document to analyze.")]
    public string FilePath { get; init; } = string.Empty;
}
