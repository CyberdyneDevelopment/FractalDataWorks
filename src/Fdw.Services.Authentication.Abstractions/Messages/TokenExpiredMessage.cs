using Fdw.Messages;
using Fdw.Services.Abstractions;
using Fdw.Messages.Attributes;

namespace Fdw.Services.Authentication.Abstractions.Messages;

/// <summary>
/// CurrentMessage indicating that a token has expired.
/// </summary>
[Message("TokenExpired")]
[MessageOption(typeof(AuthenticationMessageCollectionBase))]
public sealed class TokenExpiredMessage : AuthenticationMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TokenExpiredMessage"/> class.
    /// </summary>
    public TokenExpiredMessage()
        : base(2002, "TokenExpired", MessageSeverity.Warning,
               "The authentication token has expired", "AUTH_TOKEN_EXPIRED")
    { }

    /// <summary>
    /// Initializes a new instance with expiration details.
    /// </summary>
    /// <param name="expiredAt">When the token expired.</param>
    public TokenExpiredMessage(string expiredAt)
        : base(2002, "TokenExpired", MessageSeverity.Warning,
               $"The authentication token expired at {expiredAt}", "AUTH_TOKEN_EXPIRED")
    { }
}