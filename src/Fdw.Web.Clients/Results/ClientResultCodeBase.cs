using System.Diagnostics.CodeAnalysis;
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
}
