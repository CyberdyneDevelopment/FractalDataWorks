using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Pipelines.Webhooks.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for the run roles of a webhook trigger. Targets the pipe.WebhookTriggerRunRole table.
/// </summary>
/// <remarks>
/// Why: the configuration read and cascade-save resolve the command for a type via <c>ConfigurationCommands.All()</c>; without this
/// TypeOption they fail loud when a webhook trigger is read or saved.
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "WebhookTriggerRunRole")]
public sealed class WebhookTriggerRunRoleConfigurationCommand : ConfigurationCommandBase<WebhookTriggerRunRoleConfiguration>
{
    /// <summary>Initializes the command targeting the WebhookTriggerRunRole table.</summary>
    public WebhookTriggerRunRoleConfigurationCommand() : base("WebhookTriggerRunRole") { }
}
