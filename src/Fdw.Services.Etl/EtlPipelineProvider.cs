using System;
using Fdw.Services;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl;

/// <summary>
/// Resolves ETL pipelines by configuration name or id.
/// </summary>
public sealed class EtlPipelineProvider
    : PlatformServiceProviderBase<
          IEtlPipeline,
          IPipelineImplementationConfiguration,
          IEtlPipelineFactory<IEtlPipeline, IPipelineImplementationConfiguration>,
          IPipelineConfigurationProvider>,
      IEtlPipelineProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EtlPipelineProvider"/> class.
    /// </summary>
    /// <param name="services">The container this provider resolves factories from.</param>
    /// <param name="logger">The logger instance.</param>
    public EtlPipelineProvider(
        IServiceProvider services,
        ILogger<EtlPipelineProvider> logger)
        : base(services, logger ?? NullLogger<EtlPipelineProvider>.Instance)
    {
    }
}
