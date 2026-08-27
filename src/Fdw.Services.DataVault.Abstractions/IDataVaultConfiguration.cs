using Fdw.Configuration;

namespace Fdw.Services.DataVault.Abstractions;

/// <summary>
/// One configured data vault — the domain record, naming which vault implementation it is and holding
/// that implementation's own configuration.
/// </summary>
public interface IDataVaultConfiguration
    : IPlatformServiceConfiguration<IDataVaultImplementationConfiguration>
{
}
