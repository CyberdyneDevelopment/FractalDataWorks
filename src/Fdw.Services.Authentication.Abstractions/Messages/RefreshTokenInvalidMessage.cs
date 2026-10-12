using Fdw.Messages;
using Fdw.Services.Abstractions;
using Fdw.Messages.Attributes;

namespace Fdw.Services.Authentication.Abstractions.Messages;

/// <summary>
/// CurrentMessage indicating that a refresh token is invalid.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[Message("RefreshTokenInvalid")]
[MessageOption(typeof(AuthenticationMessageCollectionBase))]
public sealed class RefreshTokenInvalidMessage : AuthenticationMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshTokenInvalidMessage"/> class.
    /// </summary>
    public RefreshTokenInvalidMessage()
        : base(2004, "RefreshTokenInvalid", MessageSeverity.Error,
               "The refresh token is invalid or has been revoked", "AUTH_REFRESH_TOKEN_INVALID")
    { }
}