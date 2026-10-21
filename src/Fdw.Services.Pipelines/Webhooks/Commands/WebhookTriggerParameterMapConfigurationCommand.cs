using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Pipelines.Webhooks.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for the parameter mappings of a webhook trigger. Targets the pipe.WebhookTriggerParameterMap table.
/// </summary>
/// <remarks>
/// Why: the configuration read and cascade-save resolve the command for a type via <c>ConfigurationCommands.All()</c>; without this
/// TypeOption they fail loud when a webhook trigger is read or saved.
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "WebhookTriggerParameterMap")]
public sealed class WebhookTriggerParameterMapConfigurationCommand : ConfigurationCommandBase<WebhookTriggerParameterMapConfiguration>
{
    /// <summary>Initializes the command targeting the WebhookTriggerParameterMap table.</summary>
    public WebhookTriggerParameterMapConfigurationCommand() : base("WebhookTriggerParameterMap") { }
}
