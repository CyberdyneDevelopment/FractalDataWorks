using Fdw.Services.Abstractions;

namespace Fdw.Services.Multitenancy;

/// <summary>Supplies the multitenancy implementation this deployment runs.</summary>
public interface IMultitenancyConfigurationProvider
    : IDomainConfigurationProvider<IMultitenancyImplementationConfiguration>
{
}
