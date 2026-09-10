using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Pipelines;

/// <summary>Supplies the Streaming pipeline implementation's own configuration.</summary>
/// <remarks>
/// The rows live in <c>pipe.StreamingPipeline</c> and are reached through the EtlPipeline domain row that
/// names this engine. <see cref="EtlPipelineTypes"/> registers this provider into
/// <see cref="EtlPipelineConfigurationProvider"/> under the option's name.
/// </remarks>
public sealed class StreamingPipelineConfigurationProvider
    : ImplementationProviderBase<StreamingPipelineConfiguration, IEtlPipelineImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="StreamingPipelineConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    public StreamingPipelineConfigurationProvider(
        ILogger<StreamingPipelineConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName)
        : base(logger, gatewayProvider, dataStoreName, "pipe", "StreamingPipeline")
    {
    }
}
