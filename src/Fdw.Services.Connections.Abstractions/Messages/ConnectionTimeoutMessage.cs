using Fdw.Messages;
using Fdw.Messages.Attributes;

namespace Fdw.Services.Connections.Abstractions.Messages;

/// <summary>
/// CurrentMessage indicating that a connection timed out.
/// </summary>
[Message("ConnectionTimeout")]
[MessageOption(typeof(ConnectionMessageCollectionBase))]
public sealed class ConnectionTimeoutMessage : ConnectionMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionTimeoutMessage"/> class.
    /// </summary>
    public ConnectionTimeoutMessage()
        : base(3002, "ConnectionTimeout", MessageSeverity.Warning,
               "Connection attempt timed out", "CONN_TIMEOUT")
    { }

    /// <summary>
    /// Initializes a new instance with timeout duration.
    /// </summary>
    /// <param name="timeoutSeconds">The timeout duration in seconds.</param>
    public ConnectionTimeoutMessage(int timeoutSeconds)
        : base(3002, "ConnectionTimeout", MessageSeverity.Warning,
               $"Connection attempt timed out after {timeoutSeconds} seconds", "CONN_TIMEOUT")
    { }
}