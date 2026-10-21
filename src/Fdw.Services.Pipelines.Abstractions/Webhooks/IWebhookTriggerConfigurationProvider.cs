using Fdw.Services.Abstractions;

namespace Fdw.Services.Pipelines.Abstractions;

/// <summary>Supplies the webhook triggers (FDW-1884).</summary>
public interface IWebhookTriggerConfigurationProvider
    : IDomainConfigurationProvider<IWebhookTriggerImplementationConfiguration>
{
}
