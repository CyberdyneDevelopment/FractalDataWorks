using System;
using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Compilation.Commands;

/// <summary>
/// Command to get diagnostics for a document or project via compilation.
/// </summary>
[TypeOption(typeof(RoslynCommands), "GetCompilationDiagnostics")]
public sealed class GetCompilationDiagnosticsCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetCompilationDiagnosticsCommand"/> class.
    /// </summary>
    public GetCompilationDiagnosticsCommand()
        : base("GetCompilationDiagnostics", RoslynCommandCategories.Compilation, "Retrieve compilation diagnostics filtered by minimum Severity (default 'Warning'). Scope via FilePath, ProjectName, or neither (whole solution). Use as the cheapest available 'is this code valid?' probe. Returns a list of DiagnosticInfo entries.")
    {
    }

    /// <summary>
    /// Gets or sets the source file path (optional).
    /// </summary>
    [System.ComponentModel.Description("Optional file path to scope diagnostics to a single document.")]
    public string? FilePath { get; init; }

    /// <summary>
    /// Gets or sets the project name (optional).
    /// </summary>
    [System.ComponentModel.Description("Optional project name to scope diagnostics to a single project.")]
    public string? ProjectName { get; init; }

    /// <summary>
    /// Gets or sets the minimum severity (Hidden/Info/Warning/Error).
    /// </summary>
    [System.ComponentModel.Description("Minimum severity to include: 'Hidden', 'Info', 'Warning' (default), or 'Error'.")]
    public string Severity { get; init; } = "Warning";
}
