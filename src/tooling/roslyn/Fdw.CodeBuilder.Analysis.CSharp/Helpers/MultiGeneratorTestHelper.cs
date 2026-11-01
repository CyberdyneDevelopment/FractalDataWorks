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
/// Helper for running multiple source generators in sequence with proper state management.
/// Supports testing generator pipelines and dependencies.
/// </summary>
/// <remarks>
/// This code is excluded from code coverage because it is test infrastructure that supports testing but is not production code.
/// </remarks>
[ExcludeFromCodeCoverage]
public static class MultiGeneratorTestHelper
{
    /// <summary>
    /// Runs multiple generators in sequence on the same compilation.
    /// </summary>
    public static MultiGeneratorResult RunGenerators(Compilation compilation,
        CancellationToken cancellationToken,
        params IIncrementalGenerator[] generators)
    {
        if (compilation == null)
            throw new ArgumentNullException(nameof(compilation));
        if (generators == null || generators.Length == 0)
            throw new ArgumentException("At least one generator must be provided", nameof(generators));

        var result = new MultiGeneratorResult
        {
            InputCompilation = compilation,
            GeneratorResults = new Dictionary<string, GeneratorRunResult>(StringComparer.Ordinal)
        };

        var currentCompilation = compilation;
        var allDiagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

        // Run each generator in sequence
        foreach (var generator in generators)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var generatorName = generator.GetType().Name;

            // Create a new compilation with the current state
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

            // Run the generator with cancellation support
            driver = driver.RunGeneratorsAndUpdateCompilation(
                currentCompilation,
                out var outputCompilation,
                out var diagnostics,
                cancellationToken);

            // Collect diagnostics
            allDiagnostics.AddRange(diagnostics);

            // Get the run result for this specific generator
            var runResult = driver.GetRunResult();
            var generatorResult = runResult.Results.FirstOrDefault();

            if (generatorResult.Generator != null)
            {
                result.GeneratorResults[generatorName] = generatorResult;
            }

            // Update compilation for next generator
            currentCompilation = outputCompilation;
        }

        result.OutputCompilation = currentCompilation;
        result.AllDiagnostics = allDiagnostics.ToImmutable();

        return result;
    }

    /// <summary>
    /// Runs multiple generators without cancellation support.
    /// </summary>
    public static MultiGeneratorResult RunGenerators(
        Compilation compilation,
        params IIncrementalGenerator[] generators)
    {
        return RunGenerators(compilation, CancellationToken.None, generators);
    }

    /// <summary>
    /// Runs generators and verifies the output compilation has no errors.
    /// </summary>
    public static MultiGeneratorResult RunGeneratorsAndVerify(
        Compilation compilation,
        params IIncrementalGenerator[] generators)
    {
        var result = RunGenerators(compilation, generators);

        var errors = result.OutputCompilation
            .GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        if (errors.Count > 0)
        {
            var errorMessages = string.Join("\n", errors);
            throw new InvalidOperationException($"Output compilation has errors:\n{errorMessages}");
        }

        return result;
    }

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
}