using Fdw.Abstractions;
using Fdw.Services.Abstractions;

namespace Fdw.Services.SessionState;

/// <summary>
/// Factory interface for creating session state service instances.
/// </summary>
public interface ISessionStateServiceFactory : IServiceFactory<IGenericService, IServiceConfiguration>
{
}
