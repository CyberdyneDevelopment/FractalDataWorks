using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Authentication.Abstractions.Messages;

/// <summary>
/// CurrentMessage indicating that configuration binding failed.
/// </summary>
[Message("ConfigurationBindingFailed")]
[MessageOption(typeof(AuthenticationMessageCollectionBase))]
public sealed class ConfigurationBindingFailedMessage : AuthenticationMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationBindingFailedMessage"/> class.
    /// </summary>
    /// <param name="configurationTypeName">The configuration type name that failed to bind.</param>
    public ConfigurationBindingFailedMessage(string? configurationTypeName)
        : base(1007, "ConfigurationBindingFailed", MessageSeverity.Error,
               $"Failed to bind configuration to {configurationTypeName}", "AUTH_CONFIG_BINDING_FAILED")
    { }
}
