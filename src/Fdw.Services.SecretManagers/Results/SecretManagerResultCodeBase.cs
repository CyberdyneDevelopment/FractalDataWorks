using System.Diagnostics.CodeAnalysis;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.SecretManagers.Results;

/// <summary>
/// Base class for SecretManager result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class SecretManagerResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected SecretManagerResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretManagerResultCodeBase"/> class.
    /// </summary>
    protected SecretManagerResultCodeBase(
        int id,
        string name,
        string code,
        int eventId,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(id, name, code, eventId, severity, "SecretManagers", messageTemplate, isRetryable)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretManagerResultCodeBase"/> class with a categorized number.
    /// </summary>
    protected SecretManagerResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "SECRETMGR", isRetryable)
    {
    }
}
