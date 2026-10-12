using Fdw.Abstractions;
using Fdw.Services.Abstractions;

namespace Fdw.Operations;

/// <summary>
/// Factory interface for creating operations domain service instances.
/// </summary>
public interface IOperationsServiceFactory : IServiceFactory<IGenericService, IServiceConfiguration>
{
}
