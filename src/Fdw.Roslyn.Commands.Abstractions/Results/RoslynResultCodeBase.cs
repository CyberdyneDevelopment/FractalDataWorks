using System.Diagnostics.CodeAnalysis;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Base class for Roslyn command result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class RoslynResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected RoslynResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RoslynResultCodeBase"/> class.
    /// </summary>
    protected RoslynResultCodeBase(
        int id,
        string name,
        string code,
        int eventId,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(id, name, code, eventId, severity, "RoslynCommands", messageTemplate, isRetryable)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RoslynResultCodeBase"/> class from a categorized number.
    /// </summary>
    protected RoslynResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "ROSLYN", isRetryable)
    {
    }
}
