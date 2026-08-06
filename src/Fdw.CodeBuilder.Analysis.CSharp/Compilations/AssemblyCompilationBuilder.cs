using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Fdw.CodeBuilder.Analysis.CSharp.Compilations;

/// <summary>
/// Builder for creating Roslyn compilations with assembly-level attributes and metadata.
/// Supports cross-assembly reference scenarios for testing source generators.
/// </summary>
/// <remarks>
/// This code is excluded from code coverage because it is test infrastructure code that supports testing but is not production code.
/// </remarks>
[ExcludeFromCodeCoverage]
public class AssemblyCompilationBuilder
{
    private readonly List<string> _sources = [];
    private readonly List<string> _assemblyAttributes = [];
    private readonly List<MetadataReference> _references = [];
    private string _assemblyName = "TestAssembly";
    private CSharpCompilationOptions _options;

    /// <summary>
    /// Initializes a new instance of the AssemblyCompilationBuilder with default compilation options and references.
    /// </summary>
    public AssemblyCompilationBuilder()
    {
        // Default compilation options
        _options = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            optimizationLevel: OptimizationLevel.Debug,
            allowUnsafe: true,
            nullableContextOptions: NullableContextOptions.Enable);

        // Add default references using Roslyn's reference assemblies
        var referenceAssemblyPaths = new[]
        {
            typeof(object).Assembly.Location,                          // System.Private.CoreLib
            typeof(System.Linq.Enumerable).Assembly.Location,         // System.Linq
            typeof(System.Collections.Generic.List<>).Assembly.Location, // System.Collections
            typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute).Assembly.Location
        };

        foreach (var path in referenceAssemblyPaths.Where(p => !string.IsNullOrEmpty(p)))
        {
            _references.Add(MetadataReference.CreateFromFile(path));
        }
    }

    /// <summary>
    /// Adds an assembly-level attribute by type.
    /// </summary>
    public AssemblyCompilationBuilder WithAssemblyAttribute<TAttribute>() where TAttribute : Attribute
    {
        var attributeName = typeof(TAttribute).FullName;
        _assemblyAttributes.Add($"[assembly: {attributeName}]");
        return this;
    }

    /// <summary>
    /// Adds an assembly-level attribute from source code.
    /// </summary>
    public AssemblyCompilationBuilder WithAssemblyAttribute(string attributeSource)
    {
        if (!attributeSource.TrimStart().StartsWith("[assembly:", StringComparison.Ordinal))
        {
            attributeSource = $"[assembly: {attributeSource}]";
        }
        _assemblyAttributes.Add(attributeSource);
        return this;
    }

    /// <summary>
    /// Adds a reference to another compilation.
    /// </summary>
    public AssemblyCompilationBuilder WithReference(Microsoft.CodeAnalysis.Compilation compilation)
    {
        if (compilation == null)
            throw new ArgumentNullException(nameof(compilation));

        // Create a metadata reference from the compilation
        using var stream = new MemoryStream();
        var emitResult = compilation.Emit(stream);

        if (!emitResult.Success)
        {
            var errors = string.Join("\n",
                emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
            throw new InvalidOperationException($"Failed to emit reference compilation: {errors}");
        }

        stream.Seek(0, SeekOrigin.Begin);
        _references.Add(MetadataReference.CreateFromStream(stream));
        return this;
    }

    /// <summary>
    /// Adds a reference to an assembly.
    /// </summary>
    public AssemblyCompilationBuilder WithReference(Assembly assembly)
    {
        if (assembly == null)
            throw new ArgumentNullException(nameof(assembly));

        _references.Add(MetadataReference.CreateFromFile(assembly.Location));
        return this;
    }

    /// <summary>
    /// Adds a reference by metadata reference.
    /// </summary>
    public AssemblyCompilationBuilder WithReference(MetadataReference reference)
    {
        if (reference == null)
            throw new ArgumentNullException(nameof(reference));

        _references.Add(reference);
        return this;
    }

    /// <summary>
    /// Adds source code to the compilation.
    /// </summary>
    public AssemblyCompilationBuilder WithSource(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Source cannot be null or whitespace", nameof(source));

        _sources.Add(source);
        return this;
    }

    /// <summary>
    /// Sets the assembly name.
    /// </summary>
    public AssemblyCompilationBuilder WithAssemblyName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Assembly name cannot be null or whitespace", nameof(name));

        _assemblyName = name;
        return this;
    }

    /// <summary>
    /// Sets custom compilation options.
    /// </summary>
    public AssemblyCompilationBuilder WithOptions(CSharpCompilationOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        return this;
    }

    /// <summary>
    /// Builds the compilation with all configured settings.
    /// </summary>
    public Microsoft.CodeAnalysis.Compilation Build()
    {
        var syntaxTrees = new List<SyntaxTree>();

        // Combine assembly attributes with the first source file, or create a new file
        if (_assemblyAttributes.Count > 0)
        {
            var attributesSource = string.Join("\n", _assemblyAttributes);

            if (_sources.Count > 0)
            {
                // Prepend attributes to the first source
                var firstSource = attributesSource + "\n\n" + _sources[0];
                syntaxTrees.Add(CSharpSyntaxTree.ParseText(firstSource, path: "AssemblyAttributes.cs"));

                // Add remaining sources
                for (int i = 1; i < _sources.Count; i++)
                {
                    syntaxTrees.Add(CSharpSyntaxTree.ParseText(_sources[i], path: $"Source{i}.cs"));
                }
            }
            else
            {
                // Just attributes, no other source
                syntaxTrees.Add(CSharpSyntaxTree.ParseText(attributesSource, path: "AssemblyAttributes.cs"));
            }
        }
        else
        {
            // No attributes, just add sources
            for (int i = 0; i < _sources.Count; i++)
            {
                syntaxTrees.Add(CSharpSyntaxTree.ParseText(_sources[i], path: $"Source{i}.cs"));
            }
        }

        var compilation = CSharpCompilation.Create(
            _assemblyName,
            syntaxTrees,
            _references,
            _options);

        return compilation;
    }

    /// <summary>
    /// Builds and verifies the compilation has no errors.
    /// </summary>
    public Microsoft.CodeAnalysis.Compilation BuildAndVerify()
    {
        var compilation = Build();

        var diagnostics = compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        if (diagnostics.Count > 0)
        {
            var errors = string.Join("\n", diagnostics);
            throw new InvalidOperationException($"Compilation has errors:\n{errors}");
        }

        return compilation;
    }

}