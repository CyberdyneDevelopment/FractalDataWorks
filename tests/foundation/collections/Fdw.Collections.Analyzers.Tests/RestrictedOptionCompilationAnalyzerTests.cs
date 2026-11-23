using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Shouldly;
using Xunit;

namespace Fdw.Collections.Analyzers.Tests;

/// <summary>
/// Tests for <see cref="RestrictedOptionCompilationAnalyzer"/> (FDW-5136, FDW-6309): a <c>[TypeOption]</c> with
/// <c>RestrictToCurrentCompilation = true</c> whose collection is declared in another compilation is never registered, so a
/// lookup of it answers the NotFound sentinel.
/// </summary>
public sealed class RestrictedOptionCompilationAnalyzerTests
{
    private const string Attributes = @"
namespace Fdw.Collections.Attributes
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public sealed class TypeOptionAttribute : System.Attribute
    {
        public TypeOptionAttribute(System.Type collectionType, string name) { }
        public bool RestrictToCurrentCompilation { get; set; }
    }
}";

    private const string CollectionInOtherProject = @"
namespace Things
{
    public abstract partial class Containers { }
}";

    private static CSharpAnalyzerTest<RestrictedOptionCompilationAnalyzer, DefaultVerifier> TwoProjects(string optionSource)
    {
        var test = new CSharpAnalyzerTest<RestrictedOptionCompilationAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };
        test.TestState.Sources.Add(optionSource);
        test.TestState.AdditionalProjects["Collection"].Sources.Add(Attributes);
        test.TestState.AdditionalProjects["Collection"].Sources.Add(CollectionInOtherProject);
        test.TestState.AdditionalProjectReferences.Add("Collection");
        return test;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public async Task RestrictedOptionWhoseCollectionIsInAnotherCompilationIsReported()
    {
        var test = TwoProjects(@"
using Fdw.Collections.Attributes;

namespace Things
{
    [{|TC014:TypeOption(typeof(Containers), ""Table"", RestrictToCurrentCompilation = true)|}]
    public sealed class TableOption { }
}");

        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public async Task UnrestrictedOptionWhoseCollectionIsInAnotherCompilationIsNotReported()
    {
        var test = TwoProjects(@"
using Fdw.Collections.Attributes;

namespace Things
{
    [TypeOption(typeof(Containers), ""Table"")]
    public sealed class TableOption { }
}");

        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public async Task RestrictedOptionInTheCompilationOfItsCollectionIsNotReported()
    {
        var test = new CSharpAnalyzerTest<RestrictedOptionCompilationAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestCode = Attributes + CollectionInOtherProject + @"
namespace Things
{
    [Fdw.Collections.Attributes.TypeOption(typeof(Containers), ""Table"", RestrictToCurrentCompilation = true)]
    public sealed class TableOption { }
}",
        };

        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    // A collection's own constructor registers every option its compilation sees, in its assembly and in the assemblies it references,
    // whatever the option's flag says, so an option whose assembly is in the collection assembly's reference closure is not reported.
    // A real option names its collection with typeof, which makes its assembly reference the collection's, so the closure of the
    // collection's assembly can reach the option's only through a same-named collection (the match is by name), and the rule's walk is
    // tested on the graph it follows.
    private static readonly Dictionary<string, string[]> Graph = new()
    {
        ["Collection"] = ["Middle", "System.Runtime"],
        ["Middle"] = ["Options"],
        ["Options"] = ["Collection"],
        ["Unrelated"] = ["System.Runtime"],
    };

    private static IEnumerable<string> ReferencesOf(string name)
        => Graph.TryGetValue(name, out var references) ? references : Enumerable.Empty<string>();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void AnOptionsAssemblyThatTheCollectionsAssemblyReachesThroughAnotherIsInsideTheClosure()
    {
        RestrictedOptionCompilationAnalyzer.Reaches("Collection", "Options", ReferencesOf).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void AnOptionsAssemblyThatTheCollectionsAssemblyDoesNotReachIsOutsideTheClosure()
    {
        RestrictedOptionCompilationAnalyzer.Reaches("Unrelated", "Options", ReferencesOf).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void TheWalkEndsOnAReferenceCycleAndOnAnAssemblyItCannotOpen()
    {
        RestrictedOptionCompilationAnalyzer.Reaches("Options", "Missing", ReferencesOf).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void TheRuleIsTc014AndAWarningByDefault()
    {
        RestrictedOptionCompilationAnalyzer.Rule.Id.ShouldBe("TC014");
        RestrictedOptionCompilationAnalyzer.Rule.DefaultSeverity.ShouldBe(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning);
    }
}
