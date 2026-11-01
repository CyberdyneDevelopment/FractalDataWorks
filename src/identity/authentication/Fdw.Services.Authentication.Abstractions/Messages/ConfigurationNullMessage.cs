using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Authentication.Abstractions.Messages;

/// <summary>
/// CurrentMessage indicating that configuration was null.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[Message("ConfigurationNull")]
[MessageOption(typeof(AuthenticationMessageCollectionBase))]
public sealed class ConfigurationNullMessage : AuthenticationMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationNullMessage"/> class.
    /// </summary>
    public ConfigurationNullMessage()
        : base(1001, "ConfigurationNull", MessageSeverity.Error,
               "Configuration cannot be null", "AUTH_CONFIG_NULL")
    { }
}
