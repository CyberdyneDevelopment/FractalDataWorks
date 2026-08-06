using System.Diagnostics.CodeAnalysis;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Etl.Results;

/// <summary>
/// Base class for ETL result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class EtlResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected EtlResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EtlResultCodeBase"/> class.
    /// </summary>
    protected EtlResultCodeBase(
        int id,
        string name,
        string code,
        int eventId,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(id, name, code, eventId, severity, "Etl", messageTemplate, isRetryable)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EtlResultCodeBase"/> class from a categorized number.
    /// </summary>
    protected EtlResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "ETL", isRetryable)
    {
    }
}