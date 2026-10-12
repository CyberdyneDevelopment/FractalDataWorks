using Fdw.Abstractions;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Messaging;

/// <summary>
/// Factory interface for creating messaging service instances.
/// </summary>
public interface IMessagingFactory : IServiceFactory<IGenericService, IServiceConfiguration>
{
}
