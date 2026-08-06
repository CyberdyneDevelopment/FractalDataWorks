using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Fdw.CodeBuilder.Abstractions;
using Microsoft.CodeAnalysis;

namespace Fdw.CodeBuilder.Analysis.CSharp.Mocks;

/// <summary>
/// A mock implementation of a source generator that reports system errors.
/// </summary>
public class ErrorSourceGenerator : IIncrementalGenerator
{
    private readonly string _message;
    private readonly string _id;
    private readonly Action _onExceptionAction;

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorSourceGenerator"/> class.
    /// </summary>
    /// <param name="message">The error message to report.</param>
    /// <param name="id">The error ID.</param>
    /// <param name="onExceptionAction">Optional action to execute when exception is thrown.</param>
    public ErrorSourceGenerator(string message, string id = "TEST001", Action? onExceptionAction = null)
    {
        _message = message;
        _id = id;
        _onExceptionAction = onExceptionAction ?? (() => { });
    }

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(context.CompilationProvider, (spc, compilation) =>
        {
            try
            {
                // Force a specific exception
                throw new InvalidOperationException(_message);
            }
            catch (InvalidOperationException ex)
            {
                _onExceptionAction();

                // Report diagnostic
                var descriptor = new DiagnosticDescriptor(
                    _id,
                    "Test Error",
                    ex.Message,
                    "Test",
                    DiagnosticSeverity.Error,
                    true);

                spc.ReportDiagnostic(Diagnostic.Create(descriptor, Location.None));
            }
        });
    }
}