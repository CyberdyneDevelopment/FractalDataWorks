using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Shouldly;
using Xunit;

namespace Fdw.Collections.SourceGenerators.Tests;

/// <summary>
/// A collection generator over a compilation that references another project (FDW-5702).
/// </summary>
/// <remarks>
/// <para>
/// A command-line build hands a referenced project to the compiler as an assembly, so its types have no syntax. A workspace load (dotnet
/// format, the Roslyn tool) hands it as a compilation reference, whose types carry syntax of ANOTHER compilation. The generators read
/// the declared Id of every option they find in the references by binding the option's constructors, and bound a tree this compilation
/// does not hold: ArgumentException 'SyntaxTree is not part of the compilation', which the driver reports only as a warning (CS8785),
/// so the collection was silently not generated and every use of it failed to compile in the tool.
/// </para>
/// <para>
/// So each of the three generators that read option Ids is run twice over the same source, with the other project as a compilation
/// reference and as an emitted image: neither may throw, and both must give the same text. The options carry a lookup property, which
/// the discovery reads from the syntax of an option's constructor: that read must see the same option as a build does, so it takes the
/// same guard as the Id.
/// </para>
/// </remarks>
public class OptionsOfAnotherCompilationTests
{
    private static readonly string[] Kinds = ["TypeCollection", "MutableTypeCollection", "TypeInstanceCollection"];

    /// <summary>The three kinds of collection that read the declared Id of the options they find.</summary>
    public static TheoryData<string> KindNames => new(Kinds);

    [Theory]
    [MemberData(nameof(KindNames))]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void GeneratorDoesNotThrowOverOptionsOfAProjectReference(string kind)
    {
        var other = Other(kind);

        var (output, diagnostics) = RealGeneratorHelper.Run(Generator(kind), Source("B", kind), [other.ToMetadataReference()]);

        Failed(diagnostics).ShouldBeEmpty();
        output.SyntaxTrees.Count().ShouldBeGreaterThan(1, "the generator must emit the collection of B");
    }

    [Theory]
    [MemberData(nameof(KindNames))]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void OptionsOfAProjectReferenceAndOfAnEmittedImageGiveTheSameText(string kind)
    {
        var other = Other(kind);
        var image = MetadataReference.CreateFromImage(CompilationHelper.CreateAssemblyImage(other));

        var (asCompilation, _) = RealGeneratorHelper.Run(Generator(kind), Source("B", kind), [other.ToMetadataReference()]);
        var (asImage, imageDiagnostics) = RealGeneratorHelper.Run(Generator(kind), Source("B", kind), [image]);

        Failed(imageDiagnostics).ShouldBeEmpty();
        GeneratedText(asCompilation).ShouldBe(GeneratedText(asImage));
    }

    private static Compilation Other(string kind)
    {
        var (output, diagnostics) = RealGeneratorHelper.Run(Generator(kind), Source("A", kind));
        Failed(diagnostics).ShouldBeEmpty();
        RealGeneratorHelper.Errors(output).ShouldBeEmpty();
        return output;
    }

    private static List<Diagnostic> Failed(IEnumerable<Diagnostic> diagnostics) =>
        [.. diagnostics.Where(d => string.Equals(d.Id, "CS8784", StringComparison.Ordinal) || string.Equals(d.Id, "CS8785", StringComparison.Ordinal))];

    private static string GeneratedText(Compilation output) =>
        string.Join(
            "\n----\n",
            output.SyntaxTrees
                .Where(tree => tree.FilePath.Contains(".g.cs", StringComparison.Ordinal))
                .OrderBy(tree => tree.FilePath, StringComparer.Ordinal)
                .Select(tree => System.IO.Path.GetFileName(tree.FilePath) + "\n" + tree.GetText()));

    private static IIncrementalGenerator Generator(string kind) => kind switch
    {
        "TypeCollection" => new TypeCollectionGenerator(),
        "MutableTypeCollection" => new MutableTypeCollectionGenerator(),
        _ => new TypeInstanceCollectionGenerator(),
    };

    private static string Source(string name, string kind) => $$"""
        using System;
        using Fdw.Collections;
        using Fdw.Collections.Attributes;

        namespace {{name}};

        public abstract class {{name}}Base : TypeOptionBase<int, {{name}}Base>
        {
            protected {{name}}Base(int id, string name, int level) : base(id, name)
            {
                Level = level;
            }

            [TypeLookup("ByLevel")]
            public int Level { get; }
        }

        [{{kind}}(typeof({{name}}Base), typeof({{name}}Base), typeof({{name}}Items))]
        public partial class {{name}}Items : TypeCollectionBase<{{name}}Base, {{name}}Base>
        {
        }

        [TypeOption(typeof({{name}}Items), "One")]
        public sealed class {{name}}One : {{name}}Base
        {
            public {{name}}One() : base(1, "One", 7) { }
        }
        """;
}
