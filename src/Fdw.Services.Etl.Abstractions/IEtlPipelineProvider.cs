using Fdw.ServiceTypes;
using Fdw.Services.Pipelines.Abstractions;

namespace Fdw.Services.Etl.Abstractions;

/// <summary>
/// Resolves ETL pipelines by configuration name or id.
/// </summary>
public interface IEtlPipelineProvider
    : IPlatformServiceProvider<IEtlPipeline, IPipelineImplementationConfiguration>
{
}
