using Fdw.Services.Abstractions;

namespace Fdw.Services.Hosts.Abstractions;

/// <summary>
/// Reads the SQL Server endpoint domain: the domain row joined to the implementation it names.
/// </summary>
public interface ISqlServerEndpointConfigurationProvider
    : IDomainConfigurationProvider<ISqlServerEndpointImplementationConfiguration>
{
}
