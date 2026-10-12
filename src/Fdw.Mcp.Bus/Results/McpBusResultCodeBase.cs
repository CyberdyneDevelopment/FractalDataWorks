using System.Diagnostics.CodeAnalysis;
using Fdw.Results.Abstractions;

namespace Fdw.Mcp.Bus.Results;

/// <summary>
/// Base class for McpBus result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class McpBusResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected McpBusResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="McpBusResultCodeBase"/> class.
    /// </summary>
    protected McpBusResultCodeBase(
        int id,
        string name,
        string code,
        int eventId,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(id, name, code, eventId, severity, "McpBus", messageTemplate, isRetryable)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="McpBusResultCodeBase"/> class
    /// using a categorized number as the code identity.
    /// </summary>
    protected McpBusResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "SERVICES", isRetryable)
    {
    }
}