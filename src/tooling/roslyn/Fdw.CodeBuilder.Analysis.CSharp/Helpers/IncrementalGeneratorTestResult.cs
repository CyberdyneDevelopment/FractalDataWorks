using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Fdw.CodeBuilder.Analysis.CSharp.Helpers;

/// <summary>
/// Result of an incremental generator test run with tracking information.
/// </summary>
public sealed record IncrementalGeneratorTestResult(
    Compilation InitialCompilation,
    Compilation OutputCompilation,
    GeneratorDriver Driver,
    ImmutableArray<Diagnostic> Diagnostics,
    GeneratorDriverRunResult RunResult)
{
    /// <summary>
    /// Gets the first generator's run result (most common case).
    /// </summary>
    public GeneratorRunResult FirstGeneratorResult => RunResult.Results[0];

    /// <summary>
    /// Gets generated sources from the first generator.
    /// </summary>
    public ImmutableArray<GeneratedSourceResult> GeneratedSources => FirstGeneratorResult.GeneratedSources;

    /// <summary>
    /// Gets all tracked step names for debugging.
    /// </summary>
    public IEnumerable<string> TrackedStepNames => FirstGeneratorResult.TrackedSteps.Keys;

    /// <summary>
    /// Checks if the run has any error diagnostics.
    /// </summary>
    public bool HasErrors => Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

    /// <summary>
    /// Gets all error diagnostics.
    /// </summary>
    public IEnumerable<Diagnostic> Errors => Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);
}
