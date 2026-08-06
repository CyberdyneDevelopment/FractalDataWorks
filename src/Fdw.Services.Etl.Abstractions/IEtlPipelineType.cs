using System;
using Fdw.Configuration;
using Fdw.ServiceTypes;

namespace Fdw.Services.Etl.Abstractions;

/// <summary>
/// Generic interface for typed ETL pipeline type definitions.
/// </summary>
/// <typeparam name="TPipeline">The pipeline service type.</typeparam>
/// <typeparam name="TConfiguration">The configuration type.</typeparam>
/// <typeparam name="TFactory">The factory type.</typeparam>
public interface IEtlPipelineType<TPipeline, TConfiguration, TFactory> : IServiceType<Guid, TPipeline, TFactory, TConfiguration>, IEtlPipelineType
    where TPipeline : IEtlPipeline
    where TConfiguration : IGenericConfiguration
    where TFactory : IEtlPipelineFactory<TPipeline, TConfiguration>
{
}

/// <summary>
/// Non-generic interface for ETL pipeline type definitions.
/// </summary>
public interface IEtlPipelineType : IServiceType
{
}
