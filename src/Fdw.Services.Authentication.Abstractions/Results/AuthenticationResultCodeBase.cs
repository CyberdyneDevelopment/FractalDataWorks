using Fdw.Results.Abstractions;

namespace Fdw.Services.Authentication.Abstractions.Results;

/// <summary>
/// Base class for authentication result codes.
/// </summary>
public abstract class AuthenticationResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected AuthenticationResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationResultCodeBase"/> class
    /// using the categorized-number identity.
    /// </summary>
    /// <param name="number">The categorized number.</param>
    /// <param name="name">The code's name.</param>
    /// <param name="severity">The severity.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="isRetryable">Whether a caller may retry.</param>
    protected AuthenticationResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "AUTHENTICATION", isRetryable)
    {
    }
}
