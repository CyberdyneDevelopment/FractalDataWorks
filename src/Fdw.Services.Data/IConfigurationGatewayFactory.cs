using Fdw.Abstractions;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Data;

/// <summary>
/// Marker factory interface for the ConfigurationGateway service type.
/// Distinct from <see cref="IDataGatewayFactory"/> so the ServiceTypeCollection
/// generator produces a unique deterministic Id for this option.
/// </summary>
public interface IConfigurationGatewayFactory : IServiceFactory<IGenericService, IServiceConfiguration>
{
}
