using System.Diagnostics.CodeAnalysis;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Orchestration.Workflows.Results;

/// <summary>
/// Base class for Workflow result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class WorkflowResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected WorkflowResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowResultCodeBase"/> class.
    /// </summary>
    protected WorkflowResultCodeBase(
        int id,
        string name,
        string code,
        int eventId,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(id, name, code, eventId, severity, "Workflow", messageTemplate, isRetryable)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowResultCodeBase"/> class with a categorized number.
    /// </summary>
    protected WorkflowResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "WORKFLOW", isRetryable)
    {
    }
}
