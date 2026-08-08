using System.Diagnostics.CodeAnalysis;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.ServiceTypes.Results;

/// <summary>
/// Base class for ServiceType result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class ServiceTypeResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected ServiceTypeResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceTypeResultCodeBase"/> class
    /// with a categorized number identity.
    /// </summary>
    /// <param name="number">The categorized number; category is number / 10000.</param>
    /// <param name="name">The code's name.</param>
    /// <param name="severity">The severity this code carries.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="isRetryable">Whether the operation may be retried.</param>
    protected ServiceTypeResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "SERVICETYPE", isRetryable)
    {
    }
}
