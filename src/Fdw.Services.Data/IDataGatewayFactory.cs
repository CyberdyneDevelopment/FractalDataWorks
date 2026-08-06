using Fdw.Abstractions;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Data;

/// <summary>
/// Factory interface for creating DataGateway service instances.
/// </summary>
public interface IDataGatewayFactory : IServiceFactory<IGenericService, IServiceConfiguration>
{
}
