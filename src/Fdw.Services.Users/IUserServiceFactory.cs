using Fdw.Abstractions;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Users;

/// <summary>
/// Factory interface for creating user service instances.
/// </summary>
public interface IUserServiceFactory : IServiceFactory<IGenericService, IServiceConfiguration>
{
}
