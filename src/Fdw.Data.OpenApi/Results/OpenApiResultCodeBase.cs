using System.Diagnostics.CodeAnalysis;
using Fdw.Results.Abstractions;

namespace Fdw.Data.OpenApi.Results;

/// <summary>
/// Base class for OpenAPI translator result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class OpenApiResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected OpenApiResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenApiResultCodeBase"/> class.
    /// </summary>
    protected OpenApiResultCodeBase(
        int id,
        string name,
        string code,
        int eventId,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(id, name, code, eventId, severity, "OpenApi", messageTemplate, isRetryable)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenApiResultCodeBase"/> class using a categorized number.
    /// </summary>
    protected OpenApiResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "OPENAPI", isRetryable)
    {
    }
}