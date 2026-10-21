using Fdw.Services.Abstractions;
using Fdw.Services.Connections.Configuration;

namespace Fdw.Services.Connections;

/// <summary>Supplies the ConnectionGrant implementation's own configuration to the domain that registers it.</summary>
public interface IConnectionGrantImplementationConfigurationProvider
    : IImplementationConfigurationProvider<IConnectionGrantImplementationConfiguration>
{
}
