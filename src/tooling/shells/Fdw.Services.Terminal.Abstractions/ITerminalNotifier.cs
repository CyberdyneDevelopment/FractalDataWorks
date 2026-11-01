using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fdw.Services.Terminal;

/// <summary>
/// Service for notifying terminal events to clients.
/// </summary>
public interface ITerminalNotifier
{
    /// <summary>
    /// Notifies that data was received from the terminal.
    /// </summary>
    /// <param name="userId">The user ID owning the session.</param>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="data">The received data.</param>
    /// <param name="ct">Cancellation token.</param>
    Task NotifyDataReceived(Guid userId, Guid sessionId, string data, CancellationToken ct = default);

    /// <summary>
    /// Notifies that the terminal process has exited.
    /// </summary>
    /// <param name="userId">The user ID owning the session.</param>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="exitCode">The process exit code.</param>
    /// <param name="ct">Cancellation token.</param>
    Task NotifyExited(Guid userId, Guid sessionId, int exitCode, CancellationToken ct = default);
}
