using Fdw.Abstractions;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Authorization;

/// <summary>
/// Factory interface for creating authorization service instances.
/// </summary>
public interface IAuthorizationFactory : IServiceFactory<IGenericService, IServiceConfiguration>
{
}
