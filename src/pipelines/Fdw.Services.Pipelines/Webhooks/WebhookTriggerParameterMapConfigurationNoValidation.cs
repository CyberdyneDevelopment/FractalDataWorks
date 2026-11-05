using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Pipelines.Webhooks;

/// <summary>States that <see cref="WebhookTriggerParameterMapConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "WebhookTriggerParameterMapConfiguration")]
public sealed class WebhookTriggerParameterMapConfigurationNoValidation : UnvalidatedConfiguration<WebhookTriggerParameterMapConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="WebhookTriggerParameterMapConfigurationNoValidation"/> class.</summary>
    public WebhookTriggerParameterMapConfigurationNoValidation()
        : base("WebhookTriggerParameterMapConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
