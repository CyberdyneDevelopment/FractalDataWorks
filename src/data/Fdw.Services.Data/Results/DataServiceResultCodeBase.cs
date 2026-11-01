using System.Diagnostics.CodeAnalysis;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// Base class for Data Service result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class DataServiceResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected DataServiceResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataServiceResultCodeBase"/> class.
    /// </summary>
    protected DataServiceResultCodeBase(
        int id,
        string name,
        string code,
        int eventId,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(id, name, code, eventId, severity, "Data", messageTemplate, isRetryable)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataServiceResultCodeBase"/> class
    /// from a categorized number (Id == EventId == number, Code == "DATA-{number}").
    /// </summary>
    protected DataServiceResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "DATA", isRetryable)
    {
    }
}