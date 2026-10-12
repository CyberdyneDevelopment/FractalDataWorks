using Fdw.Abstractions;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Resiliency;

/// <summary>
/// Factory interface for creating resiliency service instances.
/// </summary>
public interface IResiliencyFactory : IServiceFactory<IGenericService, IServiceConfiguration>
{
}
