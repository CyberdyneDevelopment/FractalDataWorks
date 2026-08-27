using System;
using System.Collections.Generic;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Services.Etl;

/// <summary>
/// Domain-specific configuration provider for ETL pipeline configurations.
/// Thin wrapper over <see cref="ImplementationConfigurationProviderBase{TConfig,TCommand}"/> for the EtlPipeline domain.
/// Typed child configs (BatchCopy, Streaming) are resolved per ServiceTypeOption.
/// </summary>
public class EtlPipelineConfigurationProvider
    : ImplementationConfigurationProvider<
          IPipelineImplementationConfiguration,
          EtlPipelineConfiguration,
          EtlPipelineConfigurationCommand>
{

    /// <summary>Initializes a new instance of the <see cref="EtlPipelineConfigurationProvider"/> class.</summary>
    public EtlPipelineConfigurationProvider(
        ILogger<EtlPipelineConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "pipe")
        : base(logger ?? NullLogger<EtlPipelineConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName, pathName)
    {
    }
}
