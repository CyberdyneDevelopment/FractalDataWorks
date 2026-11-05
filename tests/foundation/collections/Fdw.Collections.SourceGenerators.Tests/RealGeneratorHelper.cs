using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Fdw.Collections.SourceGenerators.Tests;

/// <summary>
/// Runs one real collection generator over a source, with no mocks layered over its output.
/// </summary>
/// <remarks>
/// Why not <see cref="CompilationHelper.RunGenerator"/>: it always runs TypeCollectionGenerator and then applies
/// Mocks.MockGeneratorDriver, so what it checks is hand-written code, not what a generator emits.
/// </remarks>
internal static class RealGeneratorHelper
{
    private static readonly Lazy<List<MetadataReference>> References = new(LoadReferences);

    /// <summary>
    /// Runs <paramref name="generator"/> over <paramref name="source"/> and returns the compilation with its output
    /// added, and the diagnostics the generator reported.
    /// </summary>
    public static (Compilation Output, ImmutableArray<Diagnostic> GeneratorDiagnostics) Run(
        IIncrementalGenerator generator,
        string source)
    {
        var compilation = CSharpCompilation.Create(
            assemblyName: "SentinelGuard" + Guid.NewGuid().ToString("N"),
            syntaxTrees: [CSharpSyntaxTree.ParseText(source)],
            references: References.Value,
            options: new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));

        CSharpGeneratorDriver.Create(generator)
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);

        return (output, diagnostics);
    }

    /// <summary>
    /// Emits <paramref name="compilation"/>, loads it, and reads the static NotFound of <paramref name="collectionTypeName"/>.
    /// </summary>
    /// <remarks>
    /// Reading NotFound runs the collection's static initializer, which builds the sentinel. That is where FDW-2931
    /// threw and stopped every host.
    /// </remarks>
    public static object? ReadNotFound(Compilation compilation, string collectionTypeName)
    {
        using var stream = new MemoryStream();
        var emit = compilation.Emit(stream);
        if (!emit.Success)
        {
            throw new InvalidOperationException(
                "Emit failed:" + Environment.NewLine + string.Join(
                    Environment.NewLine,
                    emit.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)));
        }

        var collectionType = Assembly.Load(stream.ToArray()).GetType(collectionTypeName)
            ?? throw new InvalidOperationException($"The emitted assembly has no type {collectionTypeName}.");
        var notFound = collectionType.GetProperty("NotFound", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException($"{collectionTypeName} has no public static NotFound property.");

        return notFound.GetValue(null);
    }

    /// <summary>
    /// The compile errors in <paramref name="compilation"/>, generated code included.
    /// </summary>
    public static IReadOnlyList<Diagnostic> Errors(Compilation compilation)
    {
        return compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();
    }

    /// <summary>
    /// Every managed assembly the test host can load: the runtime and the test project's own dependencies,
    /// Fdw.Collections and Fdw.Services.Abstractions among them.
    /// </summary>
    private static List<MetadataReference> LoadReferences()
    {
        var trusted = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string
            ?? throw new InvalidOperationException("The test host lists no TRUSTED_PLATFORM_ASSEMBLIES.");

        return trusted
            .Split(Path.PathSeparator)
            .Where(path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToList();
    }
}
