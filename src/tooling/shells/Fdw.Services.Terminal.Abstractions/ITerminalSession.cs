using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Terminal;

/// <summary>
/// Represents a persistent terminal session.
/// </summary>
public interface ITerminalSession : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Gets the unique session identifier.
    /// </summary>
    Guid SessionId { get; }

    /// <summary>
    /// Gets the user identifier associated with this session.
    /// </summary>
    Guid UserId { get; }

    /// <summary>
    /// Gets the name of the session.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the timestamp when the session was created.
    /// </summary>
    DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// Gets the timestamp of the last activity.
    /// </summary>
    DateTimeOffset LastActivityAt { get; }

    /// <summary>
    /// Gets a value indicating whether the session is currently active.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Starts the terminal process.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<IGenericResult> Start(CancellationToken ct = default);

    /// <summary>
    /// Writes data to the terminal's standard input.
    /// </summary>
    /// <param name="data">The data to write.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<IGenericResult> Write(string data, CancellationToken ct = default);

    /// <summary>
    /// Resizes the terminal.
    /// </summary>
    /// <param name="cols">The number of columns.</param>
    /// <param name="rows">The number of rows.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<IGenericResult> Resize(int cols, int rows, CancellationToken ct = default);

    /// <summary>
    /// Occurs when data is received from the terminal's standard output or error.
    /// </summary>
    event EventHandler<TerminalDataEventArgs>? DataReceived;

    /// <summary>
    /// Occurs when the terminal process disconnects.
    /// </summary>
    event EventHandler<TerminalExitEventArgs>? Exited;
}
