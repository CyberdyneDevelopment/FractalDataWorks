using Fdw.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.SecretManagers.Abstractions.Handlers;

/// <summary>
/// Provides execution context and dependencies for secret manager command handlers.
/// </summary>
public interface ISecretManagerExecutionContext
{
    /// <summary>
    /// Gets the logger for diagnostic output.
    /// </summary>
    ILogger Logger { get; }

    /// <summary>
    /// Gets the secret manager configuration.
    /// </summary>
    IGenericConfiguration Configuration { get; }

    /// <summary>
    /// Gets the service identifier.
    /// </summary>
    string ServiceId { get; }
}
