using System.Diagnostics.CodeAnalysis;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Connections.RoslynWorkspace.Abstractions.Results;

/// <summary>
/// Base class for RoslynWorkspace connection result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class RoslynWorkspaceResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected RoslynWorkspaceResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RoslynWorkspaceResultCodeBase"/> class.
    /// </summary>
    protected RoslynWorkspaceResultCodeBase(
        int id,
        string name,
        string code,
        int eventId,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(id, name, code, eventId, severity, "RoslynWorkspace", messageTemplate, isRetryable)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RoslynWorkspaceResultCodeBase"/> class using a categorized number.
    /// </summary>
    protected RoslynWorkspaceResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "RW", isRetryable)
    {
    }
}
