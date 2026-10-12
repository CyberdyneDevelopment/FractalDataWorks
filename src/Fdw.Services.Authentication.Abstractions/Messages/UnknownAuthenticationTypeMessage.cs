using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Authentication.Abstractions.Messages;

/// <summary>
/// CurrentMessage indicating that the specified authentication type is unknown.
/// </summary>
[Message("UnknownAuthenticationType")]
[MessageOption(typeof(AuthenticationMessageCollectionBase))]
public sealed class UnknownAuthenticationTypeMessage : AuthenticationMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnknownAuthenticationTypeMessage"/> class.
    /// </summary>
    /// <param name="authenticationType">The unknown authentication type name.</param>
    public UnknownAuthenticationTypeMessage(string authenticationType)
        : base(1002, "UnknownAuthenticationType", MessageSeverity.Error,
               $"Unknown authentication type: {authenticationType}", "AUTH_UNKNOWN_TYPE")
    { }
}
