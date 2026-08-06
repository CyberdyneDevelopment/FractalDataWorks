using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Fdw.Analyzers.Tests;

/// <summary>
/// Tests for the FDW022/FDW023 <see cref="SwallowedExceptionAnalyzer"/>.
/// </summary>
public class SwallowedExceptionAnalyzerTests : AnalyzerTestBase<SwallowedExceptionAnalyzer>
{
    // Why: the analyzer skips assemblies whose name looks like a test project, so the verifier's
    // default assembly name ("TestProject") is overridden to a production-shaped name.
    private const string AssemblyName = "Fdw.Sample";

    private static CSharpAnalyzerTest<SwallowedExceptionAnalyzer, DefaultVerifier> CreateTest(string source)
    {
        return new CSharpAnalyzerTest<SwallowedExceptionAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            SolutionTransforms =
            {
                (solution, projectId) => solution.GetProject(projectId)!
                    .WithAssemblyName(AssemblyName)
                    .Solution,
            },
        };
    }

    private static async Task VerifyProductionNoDiagnostics(string source)
    {
        await CreateTest(source).RunAsync(TestContext.Current.CancellationToken);
    }

    private static async Task VerifyProductionDiagnostic(string source, string diagnosticId, DiagnosticSeverity severity)
    {
        var test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(diagnosticId, severity).WithLocation(0));
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task EmptySourceNoDiagnostics()
    {
        await VerifyProductionNoDiagnostics(string.Empty);
    }

    // ---- FDW022: swallowed exception ----

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public async Task EmptyBareCatchReportsSwallowed()
    {
        var source = @"
using System;
class Test
{
    void M()
    {
        try { Console.WriteLine(""x""); }
        {|#0:catch|} { }
    }
}";
        await VerifyProductionDiagnostic(source, SwallowedExceptionAnalyzer.SwallowedDiagnosticId, DiagnosticSeverity.Warning);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public async Task CatchWithUnusedVariableReportsSwallowed()
    {
        var source = @"
using System;
class Test
{
    void M()
    {
        try { Console.WriteLine(""x""); }
        {|#0:catch|} (Exception ex)
        {
            Console.WriteLine(""handled"");
        }
    }
}";
        await VerifyProductionDiagnostic(source, SwallowedExceptionAnalyzer.SwallowedDiagnosticId, DiagnosticSeverity.Warning);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task CatchThatObservesExceptionNoSwallowed()
    {
        // Broad catch that logs the exception: FDW022 must NOT fire (FDW023 still surveys it).
        var source = @"
using System;
class Test
{
    void M()
    {
        try { Console.WriteLine(""x""); }
        {|#0:catch|} (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}";
        await VerifyProductionDiagnostic(source, SwallowedExceptionAnalyzer.BroadCatchDiagnosticId, DiagnosticSeverity.Info);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task CatchThatRethrowsNoSwallowed()
    {
        var source = @"
using System;
class Test
{
    void M()
    {
        try { Console.WriteLine(""x""); }
        {|#0:catch|} (Exception)
        {
            throw;
        }
    }
}";
        // Rethrow → no FDW022; broad with no specific clause → FDW023 only.
        await VerifyProductionDiagnostic(source, SwallowedExceptionAnalyzer.BroadCatchDiagnosticId, DiagnosticSeverity.Info);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task SpecificCatchThatObservesExceptionNoDiagnostics()
    {
        var source = @"
using System;
class Test
{
    void M()
    {
        try { Console.WriteLine(""x""); }
        catch (FormatException ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}";
        await VerifyProductionNoDiagnostics(source);
    }

    // ---- FDW023: broad catch without specific handlers ----

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "SourceGen")]
    public async Task BroadCatchAfterSpecificCatchNoBroadDiagnostic()
    {
        // Specific clause precedes the broad catch-all and both observe the exception → no diagnostics.
        var source = @"
using System;
class Test
{
    void M()
    {
        try { Console.WriteLine(""x""); }
        catch (FormatException fex)
        {
            Console.WriteLine(fex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}";
        await VerifyProductionNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "SourceGen")]
    public async Task FilteredBroadCatchNoBroadDiagnostic()
    {
        // A 'when' filter is an explicit discrimination mechanism → FDW023 must NOT fire.
        var source = @"
using System;
class Test
{
    void M()
    {
        try { Console.WriteLine(""x""); }
        catch (Exception ex) when (ex.Message.Length > 0)
        {
            Console.WriteLine(ex.Message);
        }
    }
}";
        await VerifyProductionNoDiagnostics(source);
    }
}
