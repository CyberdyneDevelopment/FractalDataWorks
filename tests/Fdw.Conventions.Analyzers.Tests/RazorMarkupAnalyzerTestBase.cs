using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Fdw.Conventions.Analyzers.Tests;

/// <summary>
/// Base class for the .razor markup convention analyzers. These analyzers read the .razor document as an
/// additional file rather than a syntax tree, so the harness feeds markup through
/// <see cref="SolutionState.AdditionalFiles"/> and the expected diagnostics carry the .razor path.
/// </summary>
/// <typeparam name="TAnalyzer">The analyzer type to test.</typeparam>
public abstract class RazorMarkupAnalyzerTestBase<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    /// <summary>
    /// The .razor file name fed to the analyzer, and the path expected diagnostics are anchored to.
    /// </summary>
    protected const string RazorFile = "Test.razor";

    /// <summary>
    /// An assembly name the markup conventions apply to.
    /// </summary>
    protected const string InScopeAssembly = "Fdw.UI.Pages";

    /// <summary>
    /// A domain component assembly name the markup conventions apply to.
    /// </summary>
    protected const string InScopeComponentsAssembly = "Fdw.Data.UI.Components";

    /// <summary>
    /// The render package, which composes markup as its purpose and is exempt from these conventions.
    /// </summary>
    protected const string RenderingAssembly = "Fdw.UI.Rendering.Blazor";

    /// <summary>
    /// Runs the analyzer over Razor markup compiled into an in-scope assembly and verifies the diagnostics.
    /// </summary>
    /// <param name="razor">The .razor document content.</param>
    /// <param name="expected">The diagnostics the analyzer must report, and only those.</param>
    protected static Task VerifyRazor(string razor, params DiagnosticResult[] expected) =>
        VerifyRazorIn(InScopeAssembly, razor, expected);

    /// <summary>
    /// Runs the analyzer over Razor markup compiled into the named assembly and verifies the diagnostics.
    /// </summary>
    /// <param name="assemblyName">The assembly name the compilation is given.</param>
    /// <param name="razor">The .razor document content.</param>
    /// <param name="expected">The diagnostics the analyzer must report, and only those.</param>
    protected static async Task VerifyRazorIn(string assemblyName, string razor, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
        {
            TestCode = string.Empty,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.TestState.AdditionalFiles.Add((RazorFile, razor));

        test.SolutionTransforms.Add((solution, projectId) => solution.WithProjectAssemblyName(projectId, assemblyName));

        test.ExpectedDiagnostics.AddRange(expected);

        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Builds the expected diagnostic for a match of <paramref name="length"/> characters starting at the
    /// given one-based line and column of the .razor file.
    /// </summary>
    /// <param name="diagnosticId">The diagnostic ID.</param>
    /// <param name="line">The one-based line number.</param>
    /// <param name="column">The one-based column number.</param>
    /// <param name="length">The number of characters the diagnostic spans.</param>
    /// <returns>The expected diagnostic result.</returns>
    protected static DiagnosticResult RazorDiagnostic(string diagnosticId, int line, int column, int length) =>
        new DiagnosticResult(diagnosticId, DiagnosticSeverity.Warning)
            .WithSpan(RazorFile, line, column, line, column + length);
}
