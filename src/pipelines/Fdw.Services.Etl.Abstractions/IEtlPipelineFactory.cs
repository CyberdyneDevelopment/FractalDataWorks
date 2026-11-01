using Fdw.Abstractions;
using Fdw.Configuration;

namespace Fdw.Services.Etl.Abstractions;

/// <summary>
/// Marker interface for ETL pipeline factories.
/// </summary>
public interface IEtlPipelineFactory
{
}

/// <summary>
/// Generic interface for ETL pipeline factories with typed configuration.
/// </summary>
/// <typeparam name="TPipeline">The type of pipeline this factory creates.</typeparam>
/// <typeparam name="TConfiguration">The type of configuration this factory requires.</typeparam>
public interface IEtlPipelineFactory<TPipeline, TConfiguration> : IEtlPipelineFactory, IServiceFactory<TPipeline, TConfiguration>
    where TPipeline : IEtlPipeline
    where TConfiguration : IGenericConfiguration
{
}
