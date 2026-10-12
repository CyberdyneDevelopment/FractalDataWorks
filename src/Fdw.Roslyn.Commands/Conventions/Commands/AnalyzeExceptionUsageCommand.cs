using System;
using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Conventions.Commands;
/// <summary>
/// Command to analyze exception usage patterns and identify potential Result pattern candidates.
/// </summary>
[TypeOption(typeof(RoslynCommands), "AnalyzeExceptionUsage")]
public sealed class AnalyzeExceptionUsageCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AnalyzeExceptionUsageCommand"/> class.
    /// </summary>
    public AnalyzeExceptionUsageCommand()
        : base("AnalyzeExceptionUsage", RoslynCommandCategories.Conventions, "Find places that throw or catch exceptions for conditions that should use the FDW Result pattern instead. Use as part of the Result-pattern audit — exceptions are reserved for unexpected failures, not anticipated branches. Pass ProjectFilter to narrow scope. Returns a list of exception-usage sites with file/line and a suggested replacement.")
    {
    }
    /// <summary>
    /// Gets or sets the optional project filter.
    /// </summary>
    [System.ComponentModel.Description("Optional glob pattern to scope the audit to specific projects (e.g. 'Fdw.Services.*'). Null/empty audits the whole solution.")]
    public string? ProjectFilter { get; init; }
}
