using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Fdw.Services.Registration.SourceGenerators.Tests;

internal static class CompilationHelper
{
    private static readonly CSharpParseOptions ParseOptions = CSharpParseOptions.Default;

    public static Compilation CreateCompilation(
        string source,
        MetadataReference[]? additionalReferences = null,
        OutputKind outputKind = OutputKind.ConsoleApplication)
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
            // Why: IServiceProvider is type-forwarded to System.ComponentModel on .NET 10. Without this
            // reference any generated code that must BIND IServiceProvider (rather than just name it in a
            // method group) fails with CS0012. Only surfaces in tests that actually inspect
            // compilation.GetDiagnostics() — the string-assertion tests never bound it.
            MetadataReference.CreateFromFile(Assembly.Load("System.ComponentModel").Location),

            MetadataReference.CreateFromFile(typeof(Fdw.Collections.ServiceTypeCollectionAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Fdw.ServiceTypes.IServiceTypeCollection).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Fdw.ServiceTypes.PlatformServices).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Microsoft.Extensions.Hosting.IHostApplicationBuilder).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Microsoft.Extensions.Logging.ILoggerFactory).Assembly.Location),
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

    public static (Compilation OutputCompilation, ImmutableArray<Diagnostic> Diagnostics) RunGenerator(
        string source,
        MetadataReference[]? additionalReferences = null,
        OutputKind outputKind = OutputKind.ConsoleApplication)
    {
        var compilation = CreateCompilation(source, additionalReferences, outputKind);
        var generator = new PlatformServicesRegistrationGenerator();
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
