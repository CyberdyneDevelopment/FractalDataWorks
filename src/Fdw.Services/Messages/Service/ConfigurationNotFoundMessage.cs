using System.Globalization;
using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Messages;

/// <summary>
/// CurrentMessage indicating that a configuration was not found.
/// </summary>
[Message("ConfigurationNotFound")]
public sealed class ConfigurationNotFoundMessage : ServiceMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationNotFoundMessage"/> class with default message template.
    /// </summary>
    public ConfigurationNotFoundMessage()
        : base(1007, "ConfigurationNotFound", MessageSeverity.Error,
               "Configuration not found: {0}", "CONFIGURATION_NOT_FOUND")
    { }

    /// <summary>
    /// Initializes a new instance with the configuration name or ID that was not found.
    /// </summary>
    /// <param name="configurationName">The name or ID of the configuration that was not found.</param>
    public ConfigurationNotFoundMessage(string configurationName)
        : base(1007, "ConfigurationNotFound", MessageSeverity.Error,
               string.Format(CultureInfo.InvariantCulture, "Configuration not found: {0}", configurationName),
               "CONFIGURATION_NOT_FOUND")
    { }

    /// <summary>
    /// Initializes a new instance with the configuration name and service context.
    /// </summary>
    /// <param name="configurationName">The name or ID of the configuration that was not found.</param>
    /// <param name="serviceContext">Additional context about the service requiring the configuration.</param>
    public ConfigurationNotFoundMessage(string configurationName, string serviceContext)
        : base(1007, "ConfigurationNotFound", MessageSeverity.Error,
               string.Format(CultureInfo.InvariantCulture, "Configuration '{0}' not found for service: {1}", configurationName, serviceContext),
               "CONFIGURATION_NOT_FOUND")
    { }
}