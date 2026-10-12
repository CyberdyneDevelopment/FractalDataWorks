using System.Linq;
using Microsoft.CodeAnalysis;
using Shouldly;
using Xunit;

namespace Fdw.Collections.SourceGenerators.Tests;

/// <summary>
/// Covers the collision between identifiers derived from a <c>[TypeOption]</c> name and the
/// identifiers the generator emits for its own bookkeeping. Both the singleton backing field
/// (<c>_{camelCase(OptionName)}</c>) and the singleton accessor (<c>OptionName</c>) land in the
/// same member namespace as the generated infrastructure, so an option named for one of them
/// used to produce a duplicate member — surfacing as CS0102 inside generated code the author
/// never wrote.
/// </summary>
public class TypeCollectionReservedNameTests
{
    private const string Preamble = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Test;

public abstract class GateBase : TypeOptionBase<int, GateBase>
{
    protected GateBase(int id, string name) : base(id, name) { }
}

[TypeCollection(typeof(GateBase), typeof(GateBase), typeof(Gates))]
public partial class Gates : TypeCollectionBase<GateBase, GateBase>
{
}
";

    private static string WithOption(string optionName) => Preamble + $@"
[TypeOption(typeof(Gates), ""{optionName}"")]
public sealed class {optionName}Gate : GateBase
{{
    public {optionName}Gate() : base(1, ""{optionName}"") {{ }}
}}
";

    /// <summary>
    /// "Lock" camel-cases to the same identifier as the generator's own monitor field. Nothing about
    /// the name is invalid — it is a reasonable name for a gating option — so the generator must
    /// emit code that compiles rather than colliding.
    /// </summary>
    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    [InlineData("Lock")]
    [InlineData("Frozen")]
    [InlineData("Metadata")]
    [InlineData("RegisteredTypes")]
    [InlineData("PendingRegistrations")]
    public void OptionNamedForAGeneratorPrivateFieldStillCompiles(string optionName)
    {
        var (compilation, _) = CompilationHelper.RunGenerator(WithOption(optionName));

        DuplicateMemberErrors(compilation).ShouldBeEmpty();
    }

    private static string[] DuplicateMemberErrors(Compilation compilation) =>
        compilation.GetDiagnostics()
            .Where(d => d.Id is "CS0102" or "CS0111" or "CS0229")
            .Select(d => $"{d.Id}: {d.GetMessage()}")
            .ToArray();

    /// <summary>
    /// The public accessors are a different matter: an option named "All" or "NotFound" wants the
    /// exact member the collection already exposes, and no mangling can give both the name. That is
    /// an authoring error, so the generator must say so itself instead of letting the C# compiler
    /// report a duplicate member in a file the author cannot see.
    /// </summary>
    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    [InlineData("All")]
    [InlineData("NotFound")]
    [InlineData("ByName")]
    [InlineData("ById")]
    public void OptionNamedForAGeneratedPublicMemberReportsTC012(string optionName)
    {
        var (_, diagnostics) = CompilationHelper.RunGenerator(WithOption(optionName));

        var tc012 = diagnostics.SingleOrDefault(d => d.Id == "TC012");
        tc012.ShouldNotBeNull($"expected TC012 for an option named '{optionName}'");
        tc012.Severity.ShouldBe(DiagnosticSeverity.Error);
        tc012.GetMessage().ShouldContain(optionName);
    }

    /// <summary>
    /// The guard must stay narrow: an ordinary option name is not reserved and must not be
    /// diagnosed. Without this, a too-eager reserved list would break every existing collection.
    /// </summary>
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void OrdinaryOptionNameIsNotReported()
    {
        var (compilation, diagnostics) = CompilationHelper.RunGenerator(WithOption("Open"));

        diagnostics.ShouldNotContain(d => d.Id == "TC012");
        DuplicateMemberErrors(compilation).ShouldBeEmpty();
    }
}
