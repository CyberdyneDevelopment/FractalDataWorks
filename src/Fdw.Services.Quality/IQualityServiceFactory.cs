using Fdw.Abstractions;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Quality;

/// <summary>
/// Factory interface for creating quality domain service instances.
/// </summary>
public interface IQualityServiceFactory : IServiceFactory<IGenericService, IServiceConfiguration>
{
}
