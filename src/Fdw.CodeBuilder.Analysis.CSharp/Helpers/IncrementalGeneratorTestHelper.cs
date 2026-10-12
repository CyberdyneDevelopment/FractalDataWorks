using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Fdw.CodeBuilder.Analysis.CSharp.Helpers;

/// <summary>
/// Enhanced test helper for validating incremental generator performance and caching behavior.
/// Supports both standard incremental generators and cross-compilation discovery scenarios.
/// </summary>
public static class IncrementalGeneratorTestHelper
{
    /// <summary>
    /// Runs an incremental generator with full tracking enabled for performance validation.
    /// </summary>
    /// <param name="generator">The incremental generator to test.</param>
    /// <param name="sources">Source code to compile.</param>
    /// <param name="additionalReferences">Additional metadata references for the compilation.</param>
    /// <param name="enableTracking">Enable incremental step tracking for performance analysis.</param>
    /// <returns>Test result with compilation, driver, diagnostics, and run results.</returns>
    public static IncrementalGeneratorTestResult RunWithTracking(
        IIncrementalGenerator generator,
        string[] sources,
        MetadataReference[] additionalReferences,
        bool enableTracking = true)
    {
        var syntaxTrees = sources.Select(source => CSharpSyntaxTree.ParseText(source)).ToArray();

        var references = new List<MetadataReference>();

        // Add core runtime references
        AddCoreReferences(references);

        references.AddRange(additionalReferences);

        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: syntaxTrees,
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var driverOptions = new GeneratorDriverOptions(
            disabledOutputs: IncrementalGeneratorOutputKind.None,
            trackIncrementalGeneratorSteps: enableTracking);

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            driverOptions: driverOptions);

        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outputCompilation,
            out var diagnostics);

        return new IncrementalGeneratorTestResult(
            InitialCompilation: compilation,
            OutputCompilation: outputCompilation,
            Driver: driver,
            Diagnostics: diagnostics,
            RunResult: driver.GetRunResult());
    }

    /// <summary>
    /// Runs generator with cross-assembly references to test embedded generator scenarios.
    /// This simulates the scenario where a generator is packaged and discovers types across referenced assemblies.
    /// </summary>
    /// <param name="generator">The incremental generator to test.</param>
    /// <param name="currentProjectSources">Source code in the current project.</param>
    /// <param name="referencedAssemblyCompilations">Pre-compiled assemblies to reference (simulates package references).</param>
    /// <param name="enableTracking">Enable incremental step tracking.</param>
    /// <returns>Test result including cross-assembly discovery validation.</returns>
    public static IncrementalGeneratorTestResult RunWithCrossAssemblyReferences(
        IIncrementalGenerator generator,
        string[] currentProjectSources,
        Compilation[] referencedAssemblyCompilations,
        bool enableTracking = true)
    {
        var syntaxTrees = currentProjectSources.Select(source => CSharpSyntaxTree.ParseText(source)).ToArray();

        // Build metadata references from compiled assemblies
        var references = new List<MetadataReference>();

        // Add core runtime references
        AddCoreReferences(references);

        // Add references to the pre-compiled assemblies
        foreach (var referencedCompilation in referencedAssemblyCompilations)
        {
            var peStream = new System.IO.MemoryStream();
            var emitResult = referencedCompilation.Emit(peStream);
            if (!emitResult.Success)
            {
                throw new InvalidOperationException(
                    $"Failed to compile referenced assembly: {string.Join(", ", emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))}");
            }

            peStream.Position = 0;
            references.Add(MetadataReference.CreateFromStream(peStream));
        }

        var compilation = CSharpCompilation.Create(
            assemblyName: "CurrentProject",
            syntaxTrees: syntaxTrees,
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var driverOptions = new GeneratorDriverOptions(
            disabledOutputs: IncrementalGeneratorOutputKind.None,
            trackIncrementalGeneratorSteps: enableTracking);

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            driverOptions: driverOptions);

        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outputCompilation,
            out var diagnostics);

        return new IncrementalGeneratorTestResult(
            InitialCompilation: compilation,
            OutputCompilation: outputCompilation,
            Driver: driver,
            Diagnostics: diagnostics,
            RunResult: driver.GetRunResult());
    }

    /// <summary>
    /// Modifies compilation and re-runs generator to validate incremental caching behavior.
    /// </summary>
    /// <param name="previousResult">Previous test result with driver state.</param>
    /// <param name="modificationFunc">Function to modify the compilation (add/remove/change syntax trees).</param>
    /// <returns>New test result with incremental step information.</returns>
    public static IncrementalGeneratorTestResult ModifyAndRerun(
        IncrementalGeneratorTestResult previousResult,
        Func<Compilation, Compilation> modificationFunc)
    {
        var modifiedCompilation = modificationFunc(previousResult.OutputCompilation);

        var driver = previousResult.Driver.RunGeneratorsAndUpdateCompilation(
            modifiedCompilation,
            out var outputCompilation,
            out var diagnostics);

        return new IncrementalGeneratorTestResult(
            InitialCompilation: previousResult.OutputCompilation,
            OutputCompilation: outputCompilation,
            Driver: driver,
            Diagnostics: diagnostics,
            RunResult: driver.GetRunResult());
    }

    /// <summary>
    /// Adds an unrelated syntax tree to test caching of unaffected pipeline steps.
    /// </summary>
    public static Compilation AddUnrelatedClass(Compilation compilation, string className = "UnrelatedClass")
    {
        var unrelatedSource = $$"""
namespace Unrelated
{
    public class {{className}}
    {
        public void DoNothing() { }
    }
}
""";
        return compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(unrelatedSource));
    }

    /// <summary>
    /// Validates that a specific tracked pipeline step was cached (not recomputed).
    /// </summary>
    public static void AssertStepWasCached(
        GeneratorRunResult result,
        string trackingName,
        string? customMessage = null)
    {
        if (!result.TrackedSteps.TryGetValue(trackingName, out var steps))
        {
            throw new InvalidOperationException(
                $"Tracking name '{trackingName}' not found. Available: {string.Join(", ", result.TrackedSteps.Keys)}");
        }

        var step = steps.Single();
        var reason = step.Outputs.Single().Reason;

        if (reason != IncrementalStepRunReason.Cached)
        {
            var message = customMessage ?? $"Expected step '{trackingName}' to be Cached, but was {reason}";
            throw new InvalidOperationException(message);
        }
    }

    /// <summary>
    /// Validates that a specific tracked pipeline step ran with the expected reason.
    /// </summary>
    public static void AssertStepRunReason(
        GeneratorRunResult result,
        string trackingName,
        IncrementalStepRunReason expectedReason,
        string? customMessage = null)
    {
        if (!result.TrackedSteps.TryGetValue(trackingName, out var steps))
        {
            throw new InvalidOperationException(
                $"Tracking name '{trackingName}' not found. Available: {string.Join(", ", result.TrackedSteps.Keys)}");
        }

        var step = steps.Single();
        var actualReason = step.Outputs.Single().Reason;

        if (actualReason != expectedReason)
        {
            var message = customMessage ?? $"Expected step '{trackingName}' to be {expectedReason}, but was {actualReason}";
            throw new InvalidOperationException(message);
        }
    }

    /// <summary>
    /// Gets all tracked step names from the run result for debugging.
    /// </summary>
    public static IEnumerable<string> GetTrackedStepNames(GeneratorRunResult result)
    {
        return result.TrackedSteps.Keys;
    }

    /// <summary>
    /// Gets detailed information about all tracked steps for diagnostics.
    /// </summary>
    public static string GetTrackedStepsSummary(GeneratorRunResult result)
    {
        var summary = new System.Text.StringBuilder();
        summary.AppendLine("Tracked Steps Summary:");

        foreach (var kvp in result.TrackedSteps)
        {
            var name = kvp.Key;
            var steps = kvp.Value;
            summary.AppendLine($"  {name}:");
            foreach (var step in steps)
            {
                foreach (var output in step.Outputs)
                {
                    summary.AppendLine($"    - Reason: {output.Reason}");
                }
            }
        }

        return summary.ToString();
    }

    /// <summary>
    /// Adds core runtime references required for compilation.
    /// Includes System.Private.CoreLib, System.Runtime, System.Collections, and System.Linq.
    /// </summary>
    private static void AddCoreReferences(List<MetadataReference> references)
    {
        // Get the runtime directory
        var runtimePath = System.IO.Path.GetDirectoryName(typeof(object).Assembly.Location)!;

        // Add System.Private.CoreLib (contains System.Object, System.String, etc.)
        references.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        // Add System.Collections (IEnumerable<T>, etc.)
        references.Add(MetadataReference.CreateFromFile(typeof(System.Collections.Generic.IEnumerable<>).Assembly.Location));

        // Try to add System.Runtime (required in .NET 5+, especially .NET 10 preview)
        // Using try-catch instead of File.Exists to avoid analyzer warnings
        TryAddReference(references, runtimePath, "System.Runtime.dll");
        TryAddReference(references, runtimePath, "System.Linq.dll");
        TryAddReference(references, runtimePath, "System.Collections.Immutable.dll");
    }

    private static void TryAddReference(List<MetadataReference> references, string directory, string fileName)
    {
        try
        {
            var path = System.IO.Path.Combine(directory, fileName);
            references.Add(MetadataReference.CreateFromFile(path));
        }
        catch (Exception ex)
        {
            _ = ex.Message;
        }
    }
}
