using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Results.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workflows.Results;
/// <summary>
/// Base class for workflow result codes.
/// </summary>
public abstract class WorkflowResultCodeBase : ResultCodeBase
{
    private const string WorkflowDomain = "Workflow";
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected WorkflowResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref = "WorkflowResultCodeBase"/> class.
    /// </summary>
    protected WorkflowResultCodeBase(int id, string name, string code, int eventId, IResultSeverity severity, string messageTemplate, bool isRetryable) : base(id, name, code, eventId, severity, WorkflowDomain, messageTemplate, isRetryable)
    {
    }

    /// <summary>
    /// Initializes a new instance from a categorized <paramref name="number"/> (catalog scheme).
    /// </summary>
    protected WorkflowResultCodeBase(int number, string name, IResultSeverity severity, string messageTemplate, bool isRetryable = false) : base(number, name, severity, messageTemplate, "WORKFLOW", isRetryable)
    {
    }
}
