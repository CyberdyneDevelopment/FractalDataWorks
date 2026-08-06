using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Authentication.Abstractions.Messages;

/// <summary>
/// CurrentMessage indicating that configuration name was null or empty.
/// </summary>
// Why: pure message DTO; ctor only forwards literal id/severity/text to the base template, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[Message("ConfigurationNameNull")]
[MessageOption(typeof(AuthenticationMessageCollectionBase))]
public sealed class ConfigurationNameNullMessage : AuthenticationMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationNameNullMessage"/> class.
    /// </summary>
    public ConfigurationNameNullMessage()
        : base(1004, "ConfigurationNameNull", MessageSeverity.Error,
               "Configuration name cannot be null or empty", "AUTH_CONFIG_NAME_NULL")
    { }
}
