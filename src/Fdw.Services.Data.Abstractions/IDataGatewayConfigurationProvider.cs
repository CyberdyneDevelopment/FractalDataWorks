using Fdw.Services.Abstractions;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// The data gateway domain's configuration provider.
/// </summary>
/// <remarks>
/// Empty for the same reason the connection domain's provider interface is: the domain contract
/// is entirely IDomainConfigurationProvider's, and this names which domain it is so a consumer can
/// ask for one by domain rather than by closed generic.
/// </remarks>
public interface IDataGatewayConfigurationProvider
    : IDomainConfigurationProvider<IDataGatewayImplementationConfiguration>
{
}
