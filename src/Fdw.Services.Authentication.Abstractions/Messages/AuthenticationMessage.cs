using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Authentication.Abstractions.Messages;

/// <summary>
/// Base class for all authentication-related messages.
/// </summary>
// Why: pure message DTO; ctor only forwards literal id/severity/text to the base template, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[MessageCollection("AuthenticationMessages")]
public abstract class AuthenticationMessage : MessageTemplate<MessageSeverity>, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationMessage"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this message type.</param>
    /// <param name="name">The name of this message type.</param>
    /// <param name="severity">The severity level of the message.</param>
    /// <param name="message">The human-readable message text.</param>
    /// <param name="code">The message code or identifier (optional).</param>
    protected AuthenticationMessage(int id, string name, MessageSeverity severity,
        string message, string? code = null)
        : base(id, name, severity, message, code, "Authentication", null, null) { }
}