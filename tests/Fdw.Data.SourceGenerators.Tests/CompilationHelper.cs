using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Fdw.Data.SourceGenerators.Tests;

internal static class CompilationHelper
{
    private static readonly CSharpParseOptions ParseOptions = CSharpParseOptions.Default
        .WithLanguageVersion(LanguageVersion.Preview);

    public static Compilation CreateCompilation(string source, MetadataReference[]? additionalReferences = null)
    {
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.IEnumerable<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Data.Common.DbDataReader).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
            MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),

            // FDW references
            MetadataReference.CreateFromFile(typeof(Fdw.Results.GenericResult).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Fdw.Collections.ITypeOption).Assembly.Location),
        };

        // Add Data.Abstractions reference for [GenerateMapper] attribute
        try
        {
            var dataAbstractionsAssembly = Assembly.Load("Fdw.Data.Abstractions");
            references.Add(MetadataReference.CreateFromFile(dataAbstractionsAssembly.Location));
        }
        catch
        {
            // Assembly not loaded yet, will be handled by additional references
        }

        if (additionalReferences != null)
        {
            references.AddRange(additionalReferences);
        }

        var syntaxTree = CSharpSyntaxTree.ParseText(source, ParseOptions);

        return CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: new[] { syntaxTree },
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));
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

    public static (Compilation OutputCompilation, ImmutableArray<Diagnostic> Diagnostics) RunGenerator(
        string source,
        MetadataReference[]? additionalReferences = null)
    {
        var compilation = CreateCompilation(source, additionalReferences);
        var generator = new PocoMapperGenerator();
        var driver = CSharpGeneratorDriver.Create(
            generators: new[] { generator.AsSourceGenerator() },
            parseOptions: ParseOptions);
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

    public static IEnumerable<string> GetAllGeneratedFileNames(Compilation compilation)
    {
        return compilation.SyntaxTrees
            .Select(t => Path.GetFileName(t.FilePath))
            .Where(name => name.EndsWith(".g.cs"));
    }
}
