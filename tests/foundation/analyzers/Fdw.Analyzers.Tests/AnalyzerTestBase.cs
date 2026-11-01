using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Fdw.Analyzers.Tests;

/// <summary>
/// Base class for analyzer tests providing common test infrastructure.
/// </summary>
/// <typeparam name="TAnalyzer">The analyzer type to test.</typeparam>
public abstract class AnalyzerTestBase<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    /// <summary>
    /// Verifies that the analyzer does not report any diagnostics for the given source code.
    /// </summary>
    /// <param name="source">The source code to analyze.</param>
    protected static async Task VerifyNoDiagnostics(string source)
    {
        var test = new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90
        };

        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Verifies that the analyzer reports a diagnostic at the expected location.
    /// </summary>
    /// <param name="source">The source code to analyze.</param>
    /// <param name="expected">The expected diagnostic result.</param>
    protected static async Task VerifyDiagnostic(string source, DiagnosticResult expected)
    {
        var test = new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90
        };

        test.ExpectedDiagnostics.Add(expected);

        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Verifies that the analyzer reports multiple diagnostics at the expected locations.
    /// </summary>
    /// <param name="source">The source code to analyze.</param>
    /// <param name="expected">The expected diagnostic results.</param>
    protected static async Task VerifyDiagnostics(string source, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90
        };

        test.ExpectedDiagnostics.AddRange(expected);

        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Creates a DiagnosticResult for the specified diagnostic ID, line, and column.
    /// </summary>
    /// <param name="diagnosticId">The diagnostic ID.</param>
    /// <param name="line">The line number (1-based).</param>
    /// <param name="column">The column number (1-based).</param>
    /// <returns>A DiagnosticResult for the specified location.</returns>
    protected static DiagnosticResult Diagnostic(string diagnosticId, int line, int column)
    {
        return new DiagnosticResult(diagnosticId, DiagnosticSeverity.Error)
            .WithSpan(line, column, line, column);
    }

    /// <summary>
    /// Creates a DiagnosticResult for the specified diagnostic ID, severity, line, and column.
    /// </summary>
    /// <param name="diagnosticId">The diagnostic ID.</param>
    /// <param name="severity">The diagnostic severity.</param>
    /// <param name="line">The line number (1-based).</param>
    /// <param name="column">The column number (1-based).</param>
    /// <returns>A DiagnosticResult for the specified location.</returns>
    protected static DiagnosticResult Diagnostic(string diagnosticId, DiagnosticSeverity severity, int line, int column)
    {
        return new DiagnosticResult(diagnosticId, severity)
            .WithSpan(line, column, line, column);
    }
}
