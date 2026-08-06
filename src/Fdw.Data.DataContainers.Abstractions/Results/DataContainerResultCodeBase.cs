using System.Diagnostics.CodeAnalysis;
using Fdw.Results.Abstractions;

namespace Fdw.Data.DataContainers.Abstractions.Results;

/// <summary>
/// Base class for DataContainer result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class DataContainerResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected DataContainerResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataContainerResultCodeBase"/> class.
    /// </summary>
    protected DataContainerResultCodeBase(
        int id,
        string name,
        string code,
        int eventId,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(id, name, code, eventId, severity, "DataContainer", messageTemplate, isRetryable)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataContainerResultCodeBase"/> class using a categorized number.
    /// </summary>
    protected DataContainerResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "DATA", isRetryable)
    {
    }
}