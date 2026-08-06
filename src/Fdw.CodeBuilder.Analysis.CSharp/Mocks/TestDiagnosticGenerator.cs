using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Fdw.CodeBuilder.Abstractions;
using Microsoft.CodeAnalysis;

namespace Fdw.CodeBuilder.Analysis.CSharp.Mocks;

/// <summary>
/// Test generator that reports diagnostics for testing the DiagnosticReporter.
/// </summary>
public class TestDiagnosticGenerator : IIncrementalGenerator
{
    private readonly DiagnosticDescriptor _descriptor;
    private readonly Location? _location;
    private readonly object?[] _args;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestDiagnosticGenerator"/> class.
    /// </summary>
    /// <param name="descriptor">The diagnostic descriptor.</param>
    /// <param name="location">Optional location for the diagnostic.</param>
    /// <param name="args">The diagnostic message arguments.</param>
    public TestDiagnosticGenerator(
        DiagnosticDescriptor descriptor,
        Location? location = null,
        params object?[] args)
    {
        _descriptor = descriptor;
        _location = location;
        _args = args;
    }

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(context.CompilationProvider, (spc, compilation) =>
        {
            // Report the diagnostic
            var diagnostic = Diagnostic.Create(_descriptor, _location ?? Location.None, _args);
            spc.ReportDiagnostic(diagnostic);
        });
    }
}