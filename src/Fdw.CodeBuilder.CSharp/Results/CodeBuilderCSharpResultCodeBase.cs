using System.Diagnostics.CodeAnalysis;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.CodeBuilder.CSharp.Results;

/// <summary>
/// Base class for CodeBuilder CSharp result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class CodeBuilderCSharpResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected CodeBuilderCSharpResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeBuilderCSharpResultCodeBase"/> class.
    /// </summary>
    protected CodeBuilderCSharpResultCodeBase(
        int id,
        string name,
        string code,
        int eventId,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(id, name, code, eventId, severity, "CodeBuilderCSharp", messageTemplate, isRetryable)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeBuilderCSharpResultCodeBase"/> class
    /// using a categorized result-code number (Id == EventId == number).
    /// </summary>
    protected CodeBuilderCSharpResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "CODEBUILDER", isRetryable)
    {
    }
}
