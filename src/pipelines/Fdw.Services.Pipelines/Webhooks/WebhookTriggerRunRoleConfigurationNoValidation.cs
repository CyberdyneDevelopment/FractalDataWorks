using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Pipelines.Webhooks;

/// <summary>States that <see cref="WebhookTriggerRunRoleConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "WebhookTriggerRunRoleConfiguration")]
public sealed class WebhookTriggerRunRoleConfigurationNoValidation : UnvalidatedConfiguration<WebhookTriggerRunRoleConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="WebhookTriggerRunRoleConfigurationNoValidation"/> class.</summary>
    public WebhookTriggerRunRoleConfigurationNoValidation()
        : base("WebhookTriggerRunRoleConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
