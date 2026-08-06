using System.Diagnostics.CodeAnalysis;
using Fdw.Results.Abstractions;

namespace Fdw.Data.RowSources.Http.Abstractions.Results;

/// <summary>
/// Base class for HTTP RowSource result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class HttpRowSourceResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected HttpRowSourceResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpRowSourceResultCodeBase"/> class.
    /// </summary>
    protected HttpRowSourceResultCodeBase(
        int id,
        string name,
        string code,
        int eventId,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(id, name, code, eventId, severity, "HttpRowSource", messageTemplate, isRetryable)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpRowSourceResultCodeBase"/> class
    /// from a categorized number (catalog scheme).
    /// </summary>
    protected HttpRowSourceResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "HTTP", isRetryable)
    {
    }
}