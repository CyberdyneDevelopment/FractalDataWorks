using System.Diagnostics.CodeAnalysis;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Users.Results;

/// <summary>
/// Base class for User result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class UserResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected UserResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserResultCodeBase"/> class.
    /// </summary>
    protected UserResultCodeBase(
        int id,
        string name,
        string code,
        int eventId,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(id, name, code, eventId, severity, "User", messageTemplate, isRetryable)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserResultCodeBase"/> class
    /// using the categorized-number identity.
    /// </summary>
    protected UserResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "USER", isRetryable)
    {
    }
}