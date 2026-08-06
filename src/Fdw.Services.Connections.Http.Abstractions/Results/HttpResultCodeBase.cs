using System.Diagnostics.CodeAnalysis;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Connections.Http.Abstractions.Results;

/// <summary>
/// Base class for HTTP result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class HttpResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected HttpResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpResultCodeBase"/> class.
    /// </summary>
    protected HttpResultCodeBase(
        int id,
        string name,
        string code,
        int eventId,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(id, name, code, eventId, severity, "Http", messageTemplate, isRetryable)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpResultCodeBase"/> class from a categorized number.
    /// </summary>
    protected HttpResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "HTTP", isRetryable)
    {
    }
}