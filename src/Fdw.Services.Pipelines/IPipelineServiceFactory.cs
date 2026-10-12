using Fdw.Abstractions;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Pipelines;

/// <summary>
/// Factory interface for creating pipeline-service domain service instances.
/// </summary>
public interface IPipelineServiceFactory : IServiceFactory<IGenericService, IServiceConfiguration>
{
}
