using System;
using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Analysis.Commands;

/// <summary>
/// Command to retrieve compiler diagnostics for a document or project.
/// </summary>
[TypeOption(typeof(RoslynCommands), "GetDiagnostics")]
public sealed class GetDiagnosticsCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetDiagnosticsCommand"/> class.
    /// </summary>
    public GetDiagnosticsCommand()
        : base("GetDiagnostics", RoslynCommandCategories.Analysis, "Retrieve compiler diagnostics filtered by minimum Severity ('Hidden' / 'Info' / 'Warning' / 'Error', default 'Warning'). Scope is controlled by FilePath (single document) or ProjectName (project-wide); pass neither for the whole solution. Use to surface compile-time warnings/errors before attempting a refactor or build. Returns a list of DiagnosticInfo entries.")
    {
    }

    /// <summary>
    /// Gets or sets the source file path (optional).
    /// </summary>
    [System.ComponentModel.Description("Optional file path to scope diagnostics to a single document; pass with ProjectName empty for file-only scope.")]
    public string? FilePath { get; init; }

    /// <summary>
    /// Gets or sets the project name (optional).
    /// </summary>
    [System.ComponentModel.Description("Optional project name to scope diagnostics to a single project; pass with FilePath empty for project-only scope.")]
    public string? ProjectName { get; init; }

    /// <summary>
    /// Gets or sets the minimum severity (Hidden/Info/Warning/Error).
    /// </summary>
    [System.ComponentModel.Description("Minimum severity to include: 'Hidden', 'Info', 'Warning' (default), or 'Error'.")]
    public string Severity { get; init; } = "Warning";
}
