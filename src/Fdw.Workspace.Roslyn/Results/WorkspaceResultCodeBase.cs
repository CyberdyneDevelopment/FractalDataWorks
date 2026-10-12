using System.Diagnostics.CodeAnalysis;
using Fdw.Results.Abstractions;

namespace Fdw.Workspace.Roslyn.Results;

/// <summary>
/// Base class for Workspace result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class WorkspaceResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected WorkspaceResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkspaceResultCodeBase"/> class.
    /// </summary>
    protected WorkspaceResultCodeBase(
        int id,
        string name,
        string code,
        int eventId,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(id, name, code, eventId, severity, "Workspace", messageTemplate, isRetryable)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkspaceResultCodeBase"/> class
    /// from a categorized number (Id == EventId == number, Code == "WS-{number}").
    /// </summary>
    protected WorkspaceResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "WS", isRetryable)
    {
    }
}