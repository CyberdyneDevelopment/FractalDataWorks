using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Authentication.Abstractions.Messages;

/// <summary>
/// CurrentMessage indicating that token revocation failed.
/// </summary>
[Message("TokenRevocationFailed")]
[MessageOption(typeof(AuthenticationMessageCollectionBase))]
public sealed class TokenRevocationFailedMessage : AuthenticationMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TokenRevocationFailedMessage"/> class.
    /// </summary>
    /// <param name="exceptionMessage">The exception message.</param>
    public TokenRevocationFailedMessage(string exceptionMessage)
        : base(2002, "TokenRevocationFailed", MessageSeverity.Error,
               $"Failed to revoke token: {exceptionMessage}", "AUTH_REVOKE_FAILED")
    { }
}
