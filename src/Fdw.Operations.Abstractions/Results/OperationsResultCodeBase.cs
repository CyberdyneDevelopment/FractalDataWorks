using System.Diagnostics.CodeAnalysis;
using Fdw.Results.Abstractions;

namespace Fdw.Operations.Abstractions.Results;

/// <summary>
/// Base class for Operations result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class OperationsResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected OperationsResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OperationsResultCodeBase"/> class.
    /// </summary>
    protected OperationsResultCodeBase(
        int id,
        string name,
        string code,
        int eventId,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(id, name, code, eventId, severity, "Operations", messageTemplate, isRetryable)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OperationsResultCodeBase"/> class from a categorized number.
    /// </summary>
    protected OperationsResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "OPS", isRetryable)
    {
    }
}