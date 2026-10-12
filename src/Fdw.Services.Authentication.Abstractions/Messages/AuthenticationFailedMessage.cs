using Fdw.Messages;
using Fdw.Services.Abstractions;
using Fdw.Messages.Attributes;

namespace Fdw.Services.Authentication.Abstractions.Messages;

/// <summary>
/// CurrentMessage indicating that authentication failed.
/// </summary>
[Message("AuthenticationFailed")]
[MessageOption(typeof(AuthenticationMessageCollectionBase))]
public sealed class AuthenticationFailedMessage : AuthenticationMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationFailedMessage"/> class.
    /// </summary>
    public AuthenticationFailedMessage()
        : base(2003, "AuthenticationFailed", MessageSeverity.Error,
               "Authentication failed", "AUTH_FAILED")
    { }

    /// <summary>
    /// Initializes a new instance with failure reason.
    /// </summary>
    /// <param name="reason">The reason for authentication failure.</param>
    public AuthenticationFailedMessage(string reason)
        : base(2003, "AuthenticationFailed", MessageSeverity.Error,
               $"Authentication failed: {reason}", "AUTH_FAILED")
    { }
}