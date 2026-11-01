using System;
using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Analysis.Commands;

/// <summary>
/// Command to detect common code smells in source files.
/// </summary>
[TypeOption(typeof(RoslynCommands), "DetectCodeSmells")]
public sealed class DetectCodeSmellsCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DetectCodeSmellsCommand"/> class.
    /// </summary>
    public DetectCodeSmellsCommand()
        : base("DetectCodeSmells", RoslynCommandCategories.Analysis, "Run a battery of code-smell detectors over a single document (long methods, large classes, deep nesting, magic numbers, etc.). Use to surface concrete refactor targets in a file before opening it for review. Returns a list of detected smells with type, severity, file/line, and explanation.")
    {
    }

    /// <summary>
    /// Gets or sets the source file path.
    /// </summary>
    [System.ComponentModel.Description("Absolute path to the source document to scan for code smells.")]
    public string FilePath { get; init; } = string.Empty;
}
