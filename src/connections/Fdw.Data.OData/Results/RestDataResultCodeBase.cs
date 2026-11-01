using System.Diagnostics.CodeAnalysis;
using Fdw.Results.Abstractions;

namespace Fdw.Data.OData.Results;

/// <summary>
/// Base class for REST data result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class RestDataResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected RestDataResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RestDataResultCodeBase"/> class.
    /// </summary>
    protected RestDataResultCodeBase(
        int id,
        string name,
        string code,
        int eventId,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(id, name, code, eventId, severity, "RestData", messageTemplate, isRetryable)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RestDataResultCodeBase"/> class
    /// using a categorized number as the code identity.
    /// </summary>
    protected RestDataResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "REST", isRetryable)
    {
    }
}