using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Authentication.Abstractions.Messages;

/// <summary>
/// CurrentMessage indicating that a token was null or empty.
/// </summary>
// Why: pure message DTO; ctor only forwards literal id/severity/text to the base template, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[Message("TokenNullOrEmpty")]
[MessageOption(typeof(AuthenticationMessageCollectionBase))]
public sealed class TokenNullOrEmptyMessage : AuthenticationMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TokenNullOrEmptyMessage"/> class.
    /// </summary>
    public TokenNullOrEmptyMessage()
        : base(2001, "TokenNullOrEmpty", MessageSeverity.Error,
               "Token cannot be null or empty", "AUTH_TOKEN_NULL")
    { }
}
