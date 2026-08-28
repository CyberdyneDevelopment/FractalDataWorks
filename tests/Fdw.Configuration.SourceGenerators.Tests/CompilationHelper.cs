using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Fdw.Configuration.SourceGenerators;

namespace Fdw.Configuration.SourceGenerators.Tests;

/// <summary>
/// Helper for creating compilations and running source generators in tests.
/// </summary>
internal static class CompilationHelper
{
    private static readonly CSharpParseOptions ParseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);

    public static Compilation CreateCompilation(string source, MetadataReference[]? additionalReferences = null)
    {
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.IEnumerable<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
            MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Collections").Location),
        };


        if (additionalReferences != null)
        {
            references.AddRange(additionalReferences);
        }

        var withUsing = source.Contains("using Fdw.Configuration;", StringComparison.Ordinal)
            ? source
            : "using Fdw.Configuration;\n" + source;
        var syntaxTree = CSharpSyntaxTree.ParseText(withUsing, ParseOptions);

        return CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: new[] { syntaxTree },
            references: references,
            options: new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));
    }

    public static (Compilation OutputCompilation, ImmutableArray<Diagnostic> Diagnostics) RunGenerator(
        string source,
        MetadataReference[]? additionalReferences = null)
    {
        var compilation = CreateCompilation(source, additionalReferences);
        var generator = new ConfigurationSourceGenerator();
        var driver = CSharpGeneratorDriver.Create(
            generators: new[] { generator.AsSourceGenerator() },
            parseOptions: ParseOptions);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        return (outputCompilation, diagnostics);
    }

    public static string? GetGeneratedOutput(Compilation compilation, string fileName)
    {
        return compilation.SyntaxTrees
            .FirstOrDefault(t => Path.GetFileName(t.FilePath).Equals(fileName, StringComparison.Ordinal))
            ?.GetText()
            .ToString();
    }

    public static IEnumerable<string> GetAllGeneratedFiles(Compilation compilation)
    {
        return compilation.SyntaxTrees
            .Where(t => !string.IsNullOrEmpty(t.FilePath))
            .Select(t => Path.GetFileName(t.FilePath))
            .Where(f => f.EndsWith(".g.cs", StringComparison.Ordinal));
    }
}
