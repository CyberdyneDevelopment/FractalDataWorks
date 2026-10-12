using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Fdw.CodeBuilder.Abstractions;
using Microsoft.CodeAnalysis;

namespace Fdw.CodeBuilder.Analysis.CSharp.Mocks;

/// <summary>
/// A mock implementation of a source generator that throws an unhandled exception.
/// </summary>
public class UnhandledExceptionGenerator : IIncrementalGenerator
{
    private readonly string _message;
    private readonly string _exceptionType;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnhandledExceptionGenerator"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="exceptionType">The exception type to throw.</param>
    public UnhandledExceptionGenerator(string message, string exceptionType = nameof(InvalidOperationException))
    {
        _message = message;
        _exceptionType = exceptionType;
    }

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(context.CompilationProvider, (spc, compilation) =>
        {
            ThrowBasedOnType();
        });
    }

    private void ThrowBasedOnType(string message = "")
    {
        switch (_exceptionType)
        {
            case nameof(ArgumentException):
                throw new ArgumentException(_message, nameof(message));
            case nameof(NullReferenceException):
                throw new InvalidOperationException($"Null reference error: {_message}");
            case nameof(InvalidCastException):
                throw new InvalidCastException(_message);
            case nameof(NotImplementedException):
                throw new NotSupportedException(_message);
            default:
                throw new InvalidOperationException(_message);
        }
    }
}