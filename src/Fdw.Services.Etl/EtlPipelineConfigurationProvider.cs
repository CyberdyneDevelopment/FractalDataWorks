using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Commands;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Fdw.Services.Etl;

/// <summary>Supplies the EtlPipeline domain configuration.</summary>
public sealed class EtlPipelineConfigurationProvider
    : DomainConfigurationProviderBase<IEtlPipelineImplementationConfiguration>,
      IImplementationConfigurationProvider<IPipelineImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="EtlPipelineConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public EtlPipelineConfigurationProvider(
        ILogger<EtlPipelineConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "pipe", "EtlPipeline")
    {
    }
}
