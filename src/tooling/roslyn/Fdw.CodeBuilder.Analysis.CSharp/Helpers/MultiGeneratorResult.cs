using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Fdw.CodeBuilder.Analysis.CSharp.Helpers;

/// <summary>
/// Result of running multiple generators.
/// </summary>
public class MultiGeneratorResult
{
    /// <summary>
    /// The original input compilation.
    /// </summary>
    public Compilation InputCompilation { get; set; } = null!;

    /// <summary>
    /// The final compilation after all generators have run.
    /// </summary>
    public Compilation OutputCompilation { get; set; } = null!;

    /// <summary>
    /// Individual results for each generator, keyed by generator type name.
    /// </summary>
    public IDictionary<string, GeneratorRunResult> GeneratorResults { get; set; } = new Dictionary<string, GeneratorRunResult>(StringComparer.Ordinal);

    /// <summary>
    /// All diagnostics produced by all generators.
    /// </summary>
    public ImmutableArray<Diagnostic> AllDiagnostics { get; set; }

    /// <summary>
    /// Gets all generated sources from all generators.
    /// </summary>
    public IEnumerable<GeneratedSourceResult> GetAllGeneratedSources()
    {
        return GeneratorResults.Values
            .SelectMany(r => r.GeneratedSources);
    }

    /// <summary>
    /// Gets generated sources for a specific generator.
    /// </summary>
    public IEnumerable<GeneratedSourceResult> GetGeneratedSources(string generatorName)
    {
        if (GeneratorResults.TryGetValue(generatorName, out var result))
        {
            return result.GeneratedSources;
        }
        return [];
    }

    /// <summary>
    /// Checks if any generator reported diagnostics with the specified severity.
    /// </summary>
    public bool HasDiagnostics(DiagnosticSeverity severity)
    {
        return AllDiagnostics.Any(d => d.Severity == severity);
    }

    /// <summary>
    /// Gets the syntax trees added by all generators.
    /// </summary>
    public IEnumerable<SyntaxTree> GetAddedSyntaxTrees()
    {
        var originalTrees = new HashSet<SyntaxTree>(InputCompilation.SyntaxTrees);
        return OutputCompilation.SyntaxTrees.Where(t => !originalTrees.Contains(t));
    }
}