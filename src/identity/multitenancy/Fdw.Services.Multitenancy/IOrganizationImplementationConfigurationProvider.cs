using Fdw.Services.Abstractions;
using Fdw.Services.Multitenancy.Configuration;

namespace Fdw.Services.Multitenancy;

/// <summary>Supplies the organization implementation's own configuration to the domain that registers it.</summary>
public interface IOrganizationImplementationConfigurationProvider
    : IImplementationConfigurationProvider<IOrganizationImplementationConfiguration>
{
}
