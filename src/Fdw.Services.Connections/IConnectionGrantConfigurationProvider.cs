using Fdw.Services.Abstractions;
using Fdw.Services.Connections.Configuration;

namespace Fdw.Services.Connections;

/// <summary>Supplies the ConnectionGrant configuration (FDW-1581).</summary>
public interface IConnectionGrantConfigurationProvider
    : IDomainConfigurationProvider<IConnectionGrantImplementationConfiguration>
{
}
