using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Authorization.Abstractions.Messages;

/// <summary>
/// Message indicating authorization was denied.
/// </summary>
[Message("AuthorizationDenied")]
[MessageOption(typeof(AuthorizationMessage))]
public sealed class AuthorizationDeniedMessage : AuthorizationMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizationDeniedMessage"/> class.
    /// </summary>
    public AuthorizationDeniedMessage()
        : base(3001, "AuthorizationDenied", MessageSeverity.Warning,
               "Authorization denied", "AUTH_DENIED")
    { }

    /// <summary>
    /// Initializes a new instance with context.
    /// </summary>
    public AuthorizationDeniedMessage(string userId, string resource, string action)
        : base(3001, "AuthorizationDenied", MessageSeverity.Warning,
               $"User '{userId}' denied access to {resource}:{action}", "AUTH_DENIED")
    { }
}
