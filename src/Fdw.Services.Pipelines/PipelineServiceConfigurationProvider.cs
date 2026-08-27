using System;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Pipelines.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Pipelines;

/// <summary>
/// The general pipeline header provider over <c>pipe.Pipeline</c>. The full aggregate — the
/// ETL-kind typed body (pipe.EtlPipeline), its engine typed body (pipe.BatchCopyPipeline /
/// pipe.StreamingPipeline), and the kind body's Transforms — is composed on read and cascade-saved on
/// write entirely by the keystone <see cref="ImplementationConfigurationProviderBase{TConfig,TCommand}"/>. The "Etl"
/// kind typed provider is attached to this header from the Services.Etl side (the ETL domain the general header
/// consumes), mirroring the connections→secret-managers consumer-injects-provider pattern.
/// </summary>
public class PipelineServiceConfigurationProvider
    : ServiceConfigurationProviderBase<
          PipelineConfiguration,
          IPipelineImplementationConfiguration,
          PipelineConfigurationCommand>,
      IPipelineConfigurationProvider
{

    /// <summary>Initializes a new instance of the <see cref="PipelineServiceConfigurationProvider"/> class.</summary>
    public PipelineServiceConfigurationProvider(
        ILogger<PipelineServiceConfigurationProvider>? logger,
        Lazy<IConfigurationGateway> lazyGateway,
        string dataStoreName = "ConfigurationDb",
        string pathName = "pipe")
        : base(logger ?? NullLogger<PipelineServiceConfigurationProvider>.Instance,
               lazyGateway,
               dataStoreName, pathName)
    {
    }

    /// <inheritdoc />
    protected override PipelineConfiguration Compose<T>(
        string serviceOptionType,
        string name,
        T implementationConfiguration)
        => new()
        {
            Name = name,
            ServiceOptionType = serviceOptionType,
            Configuration = implementationConfiguration,
        };
}
