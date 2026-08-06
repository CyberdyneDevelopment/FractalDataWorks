using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Fdw.Registration.SourceGenerators.Tests;

internal static class CompilationHelper
{
    private static readonly CSharpParseOptions ParseOptions = CSharpParseOptions.Default;

    public static Compilation CreateCompilation(
        string source,
        MetadataReference[]? additionalReferences = null,
        OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary)
    {
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.IEnumerable<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Runtime.CompilerServices.ModuleInitializerAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
            MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),

            MetadataReference.CreateFromFile(typeof(Fdw.Collections.ITypeOption).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Fdw.Results.GenericResult).Assembly.Location),
            // Why: ConfigurationTypeOptionAttribute and ConfigurationTypes deleted in Wave C5.
            // The ConfigurationTypeModuleInitializerGenerator short-circuits when these symbols
            // are absent from the compilation — no registration code is emitted.
            MetadataReference.CreateFromFile(typeof(Fdw.Data.GenerateMapperAttribute).Assembly.Location),
        };

        if (additionalReferences != null)
        {
            references.AddRange(additionalReferences);
        }

        var syntaxTree = CSharpSyntaxTree.ParseText(source, ParseOptions);

        return CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: new[] { syntaxTree },
            references: references,
            options: new CSharpCompilationOptions(outputKind));
    }

    public static byte[] CreateAssemblyImage(Compilation compilation)
    {
        using var ms = new System.IO.MemoryStream();
        var emitResult = compilation.Emit(ms);

        if (!emitResult.Success)
        {
            var errors = string.Join(Environment.NewLine,
                emitResult.Diagnostics
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .Select(d => d.GetMessage()));

            throw new InvalidOperationException($"Compilation failed:{Environment.NewLine}{errors}");
        }

        return ms.ToArray();
    }

    public static (Compilation OutputCompilation, ImmutableArray<Diagnostic> Diagnostics) RunTypeOptionGenerator(
        string source,
        MetadataReference[]? additionalReferences = null,
        OutputKind outputKind = OutputKind.ConsoleApplication)
    {
        var compilation = CreateCompilation(source, additionalReferences, outputKind);
        var generator = new TypeOptionModuleInitializerGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        return (outputCompilation, diagnostics);
    }

    public static (Compilation OutputCompilation, ImmutableArray<Diagnostic> Diagnostics) RunServiceTypeOptionGenerator(
        string source,
        MetadataReference[]? additionalReferences = null,
        OutputKind outputKind = OutputKind.ConsoleApplication)
    {
        var compilation = CreateCompilation(source, additionalReferences, outputKind);
        var generator = new ServiceTypeOptionModuleInitializerGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        return (outputCompilation, diagnostics);
    }

    public static (Compilation OutputCompilation, ImmutableArray<Diagnostic> Diagnostics) RunConfigurationTypeGenerator(
        string source,
        MetadataReference[]? additionalReferences = null,
        OutputKind outputKind = OutputKind.ConsoleApplication)
    {
        var compilation = CreateCompilation(source, additionalReferences, outputKind);
        var generator = new ConfigurationTypeModuleInitializerGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        return (outputCompilation, diagnostics);
    }

    public static (Compilation OutputCompilation, ImmutableArray<Diagnostic> Diagnostics) RunPocoMapperGenerator(
        string source,
        MetadataReference[]? additionalReferences = null,
        OutputKind outputKind = OutputKind.ConsoleApplication)
    {
        var compilation = CreateCompilation(source, additionalReferences, outputKind);
        var generator = new PocoMapperModuleInitializerGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        return (outputCompilation, diagnostics);
    }

    public static string? GetGeneratedOutput(Compilation compilation, string fileName)
    {
        return compilation.SyntaxTrees
            .FirstOrDefault(t => Path.GetFileName(t.FilePath) == fileName)
            ?.GetText()
            .ToString();
    }
}
