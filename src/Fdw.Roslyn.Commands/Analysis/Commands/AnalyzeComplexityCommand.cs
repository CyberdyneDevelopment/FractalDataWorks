using System;
using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Analysis.Commands;

/// <summary>
/// Command to calculate cyclomatic complexity for methods.
/// </summary>
[TypeOption(typeof(RoslynCommands), "AnalyzeComplexity")]
public sealed class AnalyzeComplexityCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AnalyzeComplexityCommand"/> class.
    /// </summary>
    public AnalyzeComplexityCommand()
        : base("AnalyzeComplexity", RoslynCommandCategories.Analysis, "Calculate cyclomatic complexity per method in a single document. Use to triage refactor candidates and prioritize unit-test coverage; methods exceeding Threshold (default 10) are flagged as ExceedsThreshold. Returns Methods (all), HighComplexityMethods (over threshold), Threshold value, and counts.")
    {
    }

    /// <summary>
    /// Gets or sets the source file path.
    /// </summary>
    [System.ComponentModel.Description("Absolute path to the source document to analyze.")]
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the complexity threshold to flag.
    /// </summary>
    [System.ComponentModel.Description("Cyclomatic-complexity threshold above which methods are flagged as ExceedsThreshold (default 10).")]
    public int Threshold { get; init; } = 10;
}
