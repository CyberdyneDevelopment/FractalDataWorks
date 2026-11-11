using Fdw.Configuration;
using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Notifications.Commands;

/// <summary>ConfigurationCommands TypeOption for the Notification configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "Notification")]
public sealed class NotificationConfigurationCommand : ConfigurationCommandBase<DomainConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="NotificationConfigurationCommand"/> class.</summary>
    public NotificationConfigurationCommand() : base("Notification") { }
}
