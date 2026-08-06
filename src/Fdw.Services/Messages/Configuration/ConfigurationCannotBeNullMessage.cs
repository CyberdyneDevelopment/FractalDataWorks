using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Messages;

/// <summary>
/// CurrentMessage indicating that a required configuration parameter is null.
/// </summary>
[Message("ConfigurationCannotBeNull")]
[MessageOption(typeof(ServiceMessageCollectionBase))]
public sealed class ConfigurationCannotBeNullMessage : ServiceMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationCannotBeNullMessage"/> class.
    /// </summary>
    public ConfigurationCannotBeNullMessage()
        : base(1007, "ConfigurationCannotBeNull", MessageSeverity.Error,
               "Configuration cannot be null", "CONFIG_NULL")
    { }

    /// <summary>
    /// Initializes a new instance with the configuration parameter name.
    /// </summary>
    /// <param name="parameterName">The name of the configuration parameter that was null.</param>
    public ConfigurationCannotBeNullMessage(string parameterName)
        : base(1007, "ConfigurationCannotBeNull", MessageSeverity.Error,
               $"Configuration parameter '{parameterName}' cannot be null", "CONFIG_NULL")
    { }

    /// <summary>
    /// Initializes a new instance with parameter name and section.
    /// </summary>
    /// <param name="parameterName">The name of the configuration parameter that was null.</param>
    /// <param name="sectionName">The configuration section containing the parameter.</param>
    public ConfigurationCannotBeNullMessage(string parameterName, string sectionName)
        : base(1007, "ConfigurationCannotBeNull", MessageSeverity.Error,
               $"Configuration parameter '{parameterName}' in section '{sectionName}' cannot be null", "CONFIG_NULL")
    { }

}
