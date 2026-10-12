using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Pipelines;

/// <summary>Supplies the BatchCopy pipeline implementation's own configuration.</summary>
/// <remarks>
/// The rows live in <c>pipe.BatchCopyPipeline</c>, reached through the Pipeline domain row whose
/// <c>Implementation</c> is this engine's name. <see cref="EtlPipelineTypes"/> registers this provider into
/// the Pipeline domain provider under the option's name.
/// <para>
/// Why the contract is <see cref="IPipelineImplementationConfiguration"/> and not the narrower Etl one: the
/// domain provider only accepts a provider for ITS contract, and <c>IImplementationConfigurationProvider&lt;T&gt;</c>
/// is invariant, so a provider typed on a sub-interface is refused at registration (ProviderNotErasable) and the
/// engine is never registered.
/// </para>
/// </remarks>
public sealed class BatchCopyPipelineConfigurationProvider
    : ImplementationProviderBase<BatchCopyPipelineConfiguration, IPipelineImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="BatchCopyPipelineConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    public BatchCopyPipelineConfigurationProvider(
        ILogger<BatchCopyPipelineConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
    }
}
