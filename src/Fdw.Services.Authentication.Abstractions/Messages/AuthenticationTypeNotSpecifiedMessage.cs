using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Authentication.Abstractions.Messages;

/// <summary>
/// CurrentMessage indicating that AuthenticationType was not specified in configuration.
/// </summary>
[Message("AuthenticationTypeNotSpecified")]
[MessageOption(typeof(AuthenticationMessageCollectionBase))]
public sealed class AuthenticationTypeNotSpecifiedMessage : AuthenticationMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationTypeNotSpecifiedMessage"/> class.
    /// </summary>
    /// <param name="configurationName">The configuration section name.</param>
    public AuthenticationTypeNotSpecifiedMessage(string configurationName)
        : base(1006, "AuthenticationTypeNotSpecified", MessageSeverity.Error,
               $"AuthenticationType not specified in configuration section: {configurationName}", "AUTH_TYPE_NOT_SPECIFIED")
    { }
}
