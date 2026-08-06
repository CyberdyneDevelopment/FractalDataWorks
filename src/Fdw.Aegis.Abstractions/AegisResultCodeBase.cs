using System.Diagnostics.CodeAnalysis;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Aegis.Abstractions;

/// <summary>
/// Base class for Aegis Gateway result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class AegisResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected AegisResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AegisResultCodeBase"/> class.
    /// </summary>
    protected AegisResultCodeBase(
        int id,
        string name,
        string code,
        int eventId,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(id, name, code, eventId, severity, "Aegis", messageTemplate, isRetryable)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AegisResultCodeBase"/> class from a categorized number.
    /// </summary>
    protected AegisResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "AEG", isRetryable)
    {
    }
}
