using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Webhooks;

/// <summary>Supplies the implementation row of a webhook trigger with its run roles and parameter mappings.</summary>
public sealed class WebhookTriggerImplementationProvider
    : ImplementationProviderBase<WebhookTriggerConfiguration, IWebhookTriggerImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="WebhookTriggerImplementationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    /// <param name="pathName">The schema its rows live in.</param>
    /// <param name="containerName">The container its rows live in.</param>
    public WebhookTriggerImplementationProvider(
        ILogger<WebhookTriggerImplementationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
    }
}
