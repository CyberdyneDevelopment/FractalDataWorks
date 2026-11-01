using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Resiliency.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Resiliency.RetryNotify;

/// <summary>
/// Extended execution context for the RetryNotify strategy.
/// Provides access to the notification service for terminal failure escalation.
/// </summary>
/// <remarks>
/// Why extended context: TypeOptions must be DI-free (singleton prototypes).
/// The notification service is accessed via context to avoid constructor injection.
/// </remarks>
public interface IRetryNotifyResiliencyContext : IResiliencyExecutionContext
{
    /// <summary>
    /// Gets the logger for strategy-specific log messages.
    /// </summary>
    ILogger Logger { get; }

    /// <summary>
    /// Sends a terminal failure notification to the specified target.
    /// </summary>
    /// <param name="notificationTargetId">The notification target identifier.</param>
    /// <param name="executionId">The execution identifier for correlation.</param>
    /// <param name="message">The failure message to include in the notification.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task NotifyTerminalFailure(
        Guid notificationTargetId,
        Guid executionId,
        string message,
        CancellationToken cancellationToken);
}
