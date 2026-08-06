using System;

namespace Fdw.Services.Terminal;

/// <summary>
/// Event arguments for terminal data received events.
/// </summary>
public sealed class TerminalDataEventArgs : EventArgs
{
    /// <summary>
    /// Gets the data received from the terminal.
    /// </summary>
    public string Data { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalDataEventArgs"/> class.
    /// </summary>
    /// <param name="data">The received data.</param>
    public TerminalDataEventArgs(string data)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
    }
}
