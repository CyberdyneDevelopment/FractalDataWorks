using System.Diagnostics.CodeAnalysis;
using Fdw.Results.Abstractions;

namespace Fdw.Data.Http.Results;

/// <summary>
/// Base class for Data.Http result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class DataHttpResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected DataHttpResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataHttpResultCodeBase"/> class.
    /// </summary>
    protected DataHttpResultCodeBase(
        int id,
        string name,
        string code,
        int eventId,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(id, name, code, eventId, severity, "DataHttp", messageTemplate, isRetryable)
    {
    }

    /// <summary>
    /// Initializes a new instance from a categorized <paramref name="number"/> (catalog scheme).
    /// </summary>
    protected DataHttpResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "DATAHTTP", isRetryable)
    {
    }
}