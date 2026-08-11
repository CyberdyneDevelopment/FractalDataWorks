using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.UI.Providers;

namespace Fdw.Services.Terminal.Components.Terminal;

/// <summary>
/// Immutable context object passed to the consumer RenderFragment by <see cref="HeadlessTerminal"/>.
/// Carries both state snapshots and callback delegates so that markup can stay free of logic.
/// </summary>
public sealed class TerminalContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    /// <summary>Gets the session identifier for the active terminal session.</summary>
    public Guid SessionId { get; init; }

    /// <summary>Gets whether a session is currently active.</summary>
    public bool IsSessionActive { get; init; }


    // ── Events ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Raised when data is received from the terminal output stream.
    /// Consumers such as <see cref="XTermTerminal"/> subscribe to forward data to the UI.
    /// </summary>
    public event EventHandler<TerminalDataEventArgs>? OutputReceived;

    /// <summary>
    /// Raised when the underlying terminal process exits.
    /// </summary>
    public event EventHandler<TerminalExitEventArgs>? SessionExited;

    // ── Callbacks ──────────────────────────────────────────────────────────────

    /// <summary>Sends a command string to the terminal's standard input.</summary>
    public Func<string, CancellationToken, Task> OnSendCommand { get; init; } =
        (_, _) => Task.CompletedTask;

    /// <summary>Convenience wrapper — sends a command with no cancellation token.</summary>
    public Task SendCommand(string command, CancellationToken ct = default) =>
        OnSendCommand(command, ct);

    // ── Internal raise helpers (called by HeadlessTerminal only) ───────────────

    internal void RaiseOutputReceived(TerminalDataEventArgs args) =>
        OutputReceived?.Invoke(this, args);

    internal void RaiseSessionExited(TerminalExitEventArgs args) =>
        SessionExited?.Invoke(this, args);
}
