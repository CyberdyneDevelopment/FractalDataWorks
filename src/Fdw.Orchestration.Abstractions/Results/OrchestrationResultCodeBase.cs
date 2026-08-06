using System.Diagnostics.CodeAnalysis;
using Fdw.Results.Abstractions;

namespace Fdw.Orchestration.Abstractions.Results;

/// <summary>
/// Base class for orchestration result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class OrchestrationResultCodeBase : ResultCodeBase, IOrchestrationResultCode
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected OrchestrationResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrchestrationResultCodeBase"/> class.
    /// </summary>
    protected OrchestrationResultCodeBase(
        int id,
        string name,
        string code,
        int eventId,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(id, name, code, eventId, severity, "Orchestration", messageTemplate, isRetryable)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrchestrationResultCodeBase"/> class using a categorized number.
    /// </summary>
    protected OrchestrationResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "ORCH", isRetryable)
    {
    }
}