using System.Diagnostics.CodeAnalysis;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Results;

/// <summary>
/// Base class for Services result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class ServicesResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected ServicesResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServicesResultCodeBase"/> class.
    /// </summary>
    protected ServicesResultCodeBase(
        int id,
        string name,
        string code,
        int eventId,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(id, name, code, eventId, severity, "Services", messageTemplate, isRetryable)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServicesResultCodeBase"/> class
    /// using a categorized number as the code identity.
    /// </summary>
    protected ServicesResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "SERVICES", isRetryable)
    {
    }
}