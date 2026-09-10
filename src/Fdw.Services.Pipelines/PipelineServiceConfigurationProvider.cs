using Fdw.Configuration;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Pipelines.Commands;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines;

/// <summary>Supplies the Pipeline configuration.</summary>
public sealed class PipelineServiceConfigurationProvider
    : DomainConfigurationProviderBase<IPipelineImplementationConfiguration>,
      IPipelineConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="PipelineServiceConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public PipelineServiceConfigurationProvider(
        ILogger<PipelineServiceConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "pipe", "Pipeline")
    {
    }
}
