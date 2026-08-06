using System.Diagnostics.CodeAnalysis;
using Fdw.Results.Abstractions;

namespace Fdw.Data.DataStores.Rest.Results;

/// <summary>
/// Base class for REST DataStore result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class RestDataStoreResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected RestDataStoreResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RestDataStoreResultCodeBase"/> class.
    /// </summary>
    protected RestDataStoreResultCodeBase(
        int id,
        string name,
        string code,
        int eventId,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(id, name, code, eventId, severity, "RestDataStore", messageTemplate, isRetryable)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RestDataStoreResultCodeBase"/> class
    /// from a categorized number (Id == EventId == number, Code == "REST-{number}").
    /// </summary>
    protected RestDataStoreResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "REST", isRetryable)
    {
    }
}