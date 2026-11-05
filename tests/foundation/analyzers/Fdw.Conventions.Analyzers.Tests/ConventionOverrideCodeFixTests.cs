using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Conventions.CodeFixes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Shouldly;
using Xunit;

namespace Fdw.Conventions.Analyzers.Tests;

/// <summary>
/// The [ConventionOverride] code fix for FDW006 and FDW007 (FDW-3504): it writes exactly the value the analyzer
/// measured, replaces a value already written, lands where the analyzer reads it, and offers nothing without a measure.
/// </summary>
public class ConventionOverrideCodeFixTests
{
    private const int LineThreshold = 60;

    private const string Header = """
        using Fdw.Conventions;

        namespace Fdw.Conventions
        {
            [System.AttributeUsage(System.AttributeTargets.Method | System.AttributeTargets.Constructor | System.AttributeTargets.Class)]
            public sealed class ConventionOverrideAttribute : System.Attribute
            {
                public int MaxMethodLines { get; set; } = -1;
                public int MaxCyclomaticComplexity { get; set; } = -1;
            }
        }

        """;

    /// <summary>
    /// The line ending the raw-string sources carry (the test file's own), so generated lines match them and the
    /// fixed text compares equal however the file is checked out.
    /// </summary>
    private static readonly string LineEnding = Header.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";

    private static string Filler(int count, int indent) =>
        string.Join(
            LineEnding,
            Enumerable.Range(0, count).Select(index => new string(' ', indent) + $"System.Console.WriteLine(\"{index}\");"));

    private static string Branches(int count) =>
        string.Join(
            LineEnding,
            Enumerable.Range(0, count).Select(index => $"        if (x == {index}) x = {index + 1};"));

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task MethodTooLongFixWritesTheMeasuredLineCount()
    {
        var test = new CSharpCodeFixTest<MethodTooLongAnalyzer, SuppressWithConventionOverrideCodeFixProvider, DefaultVerifier>
        {
            TestCode = Header + $$"""
                public class Sample
                {
                    public void {|#0:LongMethod|}()
                    {
                {{Filler(LineThreshold + 1, 8)}}
                    }
                }
                """,
            FixedCode = Header + $$"""
                public class Sample
                {
                    [ConventionOverride(MaxMethodLines = {{LineThreshold + 1}})]
                    public void LongMethod()
                    {
                {{Filler(LineThreshold + 1, 8)}}
                    }
                }
                """,
        };
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("FDW006", DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("LongMethod", LineThreshold + 1, LineThreshold));

        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task MethodTooComplexFixWritesTheMeasuredComplexity()
    {
        // Complexity is 1 plus one per if: 11 ifs measure 12, over the default threshold of 10.
        var test = new CSharpCodeFixTest<MethodTooComplexAnalyzer, SuppressWithConventionOverrideCodeFixProvider, DefaultVerifier>
        {
            TestCode = Header + $$"""
                public class Sample
                {
                    public void {|#0:Branchy|}(int x)
                    {
                {{Branches(11)}}
                    }
                }
                """,
            FixedCode = Header + $$"""
                public class Sample
                {
                    [ConventionOverride(MaxCyclomaticComplexity = 12)]
                    public void Branchy(int x)
                    {
                {{Branches(11)}}
                    }
                }
                """,
        };
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("FDW007", DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("Branchy", 12, 10));

        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task ReapplyingTheFixReplacesTheValueAlreadyWritten()
    {
        var test = new CSharpCodeFixTest<MethodTooLongAnalyzer, SuppressWithConventionOverrideCodeFixProvider, DefaultVerifier>
        {
            TestCode = Header + $$"""
                public class Sample
                {
                    [ConventionOverride(MaxMethodLines = 50)]
                    public void {|#0:LongMethod|}()
                    {
                {{Filler(LineThreshold + 1, 8)}}
                    }
                }
                """,
            FixedCode = Header + $$"""
                public class Sample
                {
                    [ConventionOverride(MaxMethodLines = {{LineThreshold + 1}})]
                    public void LongMethod()
                    {
                {{Filler(LineThreshold + 1, 8)}}
                    }
                }
                """,
        };
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("FDW006", DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("LongMethod", LineThreshold + 1, 50));

        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task ParenthesizedLambdaFixLandsOnTheLambdaAndClearsIt()
    {
        var test = new CSharpCodeFixTest<MethodTooLongAnalyzer, SuppressWithConventionOverrideCodeFixProvider, DefaultVerifier>
        {
            TestCode = Header + $$"""
                public class Sample
                {
                    public Sample()
                    {
                        System.Action body = {|#0:(|}) =>
                        {
                {{Filler(LineThreshold + 1, 12)}}
                        };
                        body();
                    }
                }
                """,
            FixedCode = Header + $$"""
                public class Sample
                {
                    public Sample()
                    {
                        System.Action body = [ConventionOverride(MaxMethodLines = {{LineThreshold + 1}})] () =>
                        {
                {{Filler(LineThreshold + 1, 12)}}
                        };
                        body();
                    }
                }
                """,
        };
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("FDW006", DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("Sample (lambda)", LineThreshold + 1, LineThreshold));

        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task SimpleLambdaGetsNoFixBecauseItCannotCarryTheAttribute()
    {
        var source = Header + $$"""
            public class Sample
            {
                public Sample()
                {
                    System.Action<int> body = {|#0:value|} =>
                    {
            {{Filler(LineThreshold + 1, 12)}}
                    };
                    body(0);
                }
            }
            """;
        var test = new CSharpCodeFixTest<MethodTooLongAnalyzer, SuppressWithConventionOverrideCodeFixProvider, DefaultVerifier>
        {
            TestCode = source,
            FixedCode = source,
        };
        var expected = new DiagnosticResult("FDW006", DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("Sample (lambda)", LineThreshold + 1, LineThreshold);
        test.ExpectedDiagnostics.Add(expected);
        test.FixedState.ExpectedDiagnostics.Add(expected);

        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public async Task DiagnosticWithoutAMeasuredValueGetsNoFix()
    {
        // The provider is called directly with an FDW006 diagnostic that carries no ActualValue, the case the
        // analyzers never produce. Its descriptor is the real analyzer's, so the test declares no analyzer of its own.
        var source = Header + """
            public class Sample
            {
                public void Anything()
                {
                    System.Console.WriteLine("0");
                }
            }
            """;
        using var workspace = new AdhocWorkspace();
        var document = workspace.CurrentSolution
            .AddProject("NoMeasure", "NoMeasure", LanguageNames.CSharp)
            .AddDocument("Test0.cs", source);
        var root = await document.GetSyntaxRootAsync(TestContext.Current.CancellationToken)
            ?? throw new InvalidOperationException("The test document has no syntax root.");
        var identifier = root.DescendantTokens()
            .First(token => string.Equals(token.ValueText, "Anything", StringComparison.Ordinal));
        var diagnostic = Diagnostic.Create(
            new MethodTooLongAnalyzer().SupportedDiagnostics[0],
            identifier.GetLocation(),
            "Anything",
            LineThreshold + 1,
            LineThreshold);
        var offered = new System.Collections.Generic.List<CodeAction>();
        var context = new CodeFixContext(
            document,
            diagnostic,
            (action, _) => offered.Add(action),
            TestContext.Current.CancellationToken);

        await new SuppressWithConventionOverrideCodeFixProvider().RegisterCodeFixesAsync(context);

        offered.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task BothAnalyzersPutTheMeasuredValueOnTheDiagnostic()
    {
        var source = $$"""
            public class Sample
            {
                public void LongMethod()
                {
            {{Filler(LineThreshold + 1, 8)}}
                }

                public void Branchy(int x)
                {
            {{Branches(11)}}
                }
            }
            """;

        var diagnostics = await AnalyzerDiagnostics(source, new MethodTooLongAnalyzer(), new MethodTooComplexAnalyzer());

        diagnostics.Single(d => string.Equals(d.Id, "FDW006", StringComparison.Ordinal))
            .Properties["ActualValue"].ShouldBe((LineThreshold + 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
        diagnostics.Single(d => string.Equals(d.Id, "FDW007", StringComparison.Ordinal))
            .Properties["ActualValue"].ShouldBe("12");
    }

    private static async Task<System.Collections.Immutable.ImmutableArray<Diagnostic>> AnalyzerDiagnostics(
        string source,
        params DiagnosticAnalyzer[] analyzers)
    {
        var trusted = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string
            ?? throw new InvalidOperationException("The test host lists no TRUSTED_PLATFORM_ASSEMBLIES.");
        var compilation = CSharpCompilation.Create(
            "MeasuredValue",
            [CSharpSyntaxTree.ParseText(source, cancellationToken: TestContext.Current.CancellationToken)],
            trusted.Split(Path.PathSeparator)
                .Where(path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                .Select(path => MetadataReference.CreateFromFile(path)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return await compilation
            .WithAnalyzers([.. analyzers])
            .GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);
    }
}
