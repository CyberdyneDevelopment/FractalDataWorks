using System.Diagnostics.CodeAnalysis;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Web.Clients.Abstractions.Results;

/// <summary>
/// Base class for HTTP API client result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class ClientResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected ClientResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientResultCodeBase"/> class from a categorized number.
    /// </summary>
    protected ClientResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "CLIENT", isRetryable)
    {
    }

    /// <summary>
    /// The reason the downstream service gave for refusing the call, as the client captured it, or none when the service gave none. A code that hands it to the
    /// caller returns it from <c>FormatClientMessage</c>: the downstream wrote it for the caller, and nothing is invented when it did not.
    /// </summary>
    /// <param name="details">The details of the failure.</param>
    /// <returns>The downstream's reason, or none.</returns>
    protected static string? RelayedReason(IResultDetails? details) =>
        details is not null && details.Data.TryGetValue("reason", out var reason) ? reason?.ToString() : null;
}
