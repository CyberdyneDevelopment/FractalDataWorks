using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Notifications.Webhook.Commands;

/// <summary>ConfigurationCommands TypeOption for the WebhookNotification configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "WebhookNotification")]
public sealed class WebhookNotificationConfigurationCommand : ConfigurationCommandBase<WebhookNotificationConfiguration>
{
    /// <inheritdoc/>
    public WebhookNotificationConfigurationCommand() : base("WebhookNotification") { }
}
