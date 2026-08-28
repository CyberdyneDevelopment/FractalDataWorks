using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Connections.Abstractions.Messages;

/// <summary>
/// Base class for connection service messages.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[MessageCollection("ConnectionMessages")]
public abstract class ConnectionMessage : MessageTemplate<MessageSeverity>, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionMessage"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this message.</param>
    /// <param name="name">The name of this message.</param>
    /// <param name="severity">The severity level of the message.</param>
    /// <param name="message">The human-readable message text.</param>
    /// <param name="code">The unique error code for this message.</param>
    protected ConnectionMessage(int id, string name, MessageSeverity severity, string message, string? code = null)
        : base(id, name, severity, message, code, "Connections", null, null)
    {
    }
}