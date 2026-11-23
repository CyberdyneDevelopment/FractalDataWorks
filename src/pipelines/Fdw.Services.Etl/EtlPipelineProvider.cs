using Fdw.Services.Abstractions;
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
    : DomainServiceProviderBase<
          IEtlPipeline,
          IPipelineImplementationConfiguration,
          IEtlPipelineFactory<IEtlPipeline, IPipelineImplementationConfiguration>,
          IPipelineConfigurationProvider>,
      IEtlPipelineProvider
{
    /// <summary>Gets how this provider reads the configuration it builds a service from.</summary>
    /// <remarks>A pipeline is built to run from its configuration (its type, its components, its settings) and shows no description, and nothing saves the configuration back (FDW-7323).</remarks>
    protected override ConfigurationReadOptions ReadOptions => ConfigurationReadOptions.WithoutFilledMembers;

    /// <summary>
    /// Initializes a new instance of the <see cref="EtlPipelineProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public EtlPipelineProvider(
        ILogger<EtlPipelineProvider> logger)
        : base(logger ?? NullLogger<EtlPipelineProvider>.Instance)
    {
    }
}
