using Fdw.Services.Abstractions;

namespace Fdw.Services.DataVault.Abstractions;

/// <summary>
/// Resolves configured data vaults and routes each to the implementation provider that owns it.
/// </summary>
public interface IDataVaultConfigurationProvider
    : IDomainConfigurationProvider<IDataVaultImplementationConfiguration>
{
}
