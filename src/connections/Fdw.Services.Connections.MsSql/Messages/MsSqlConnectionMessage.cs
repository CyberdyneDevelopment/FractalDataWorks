using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Connections.Abstractions.Messages;

namespace Fdw.Services.Connections.MsSql.Messages;

/// <summary>
/// Base class for MS SQL connection-specific messages.
/// </summary>
[MessageCollection("MsSqlConnectionMessages")]
public abstract class MsSqlConnectionMessage : ConnectionMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlConnectionMessage"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this message.</param>
    /// <param name="name">The name of this message.</param>
    /// <param name="severity">The severity level of the message.</param>
    /// <param name="message">The human-readable message text.</param>
    /// <param name="code">The unique error code for this message.</param>
    protected MsSqlConnectionMessage(int id, string name, MessageSeverity severity, string message, string? code = null)
        : base(id, name, severity, message, code)
    {
    }
}
