using Fdw.Messages;
using Fdw.Messages.Attributes;

namespace Fdw.Services.Connections.Abstractions.Messages;

/// <summary>
/// CurrentMessage indicating that a connection attempt failed.
/// </summary>
[Message("ConnectionFailed")]
[MessageOption(typeof(ConnectionMessageCollectionBase))]
public sealed class ConnectionFailedMessage : ConnectionMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionFailedMessage"/> class.
    /// </summary>
    public ConnectionFailedMessage()
        : base(3001, "ConnectionFailed", MessageSeverity.Error,
               "Failed to establish connection", "CONN_FAILED")
    { }

    /// <summary>
    /// Initializes a new instance with specific error details.
    /// </summary>
    /// <param name="reason">The reason why the connection failed.</param>
    public ConnectionFailedMessage(string reason)
        : base(3001, "ConnectionFailed", MessageSeverity.Error,
               $"Failed to establish connection: {reason}", "CONN_FAILED")
    { }
}