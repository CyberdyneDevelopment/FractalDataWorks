using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Notifications.Console.Commands;

/// <summary>ConfigurationCommands TypeOption for the ConsoleNotification configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "ConsoleNotification")]
public sealed class ConsoleNotificationConfigurationCommand : ConfigurationCommandBase<ConsoleNotificationConfiguration>
{
    /// <inheritdoc/>
    public ConsoleNotificationConfigurationCommand() : base("ConsoleNotification") { }
}
