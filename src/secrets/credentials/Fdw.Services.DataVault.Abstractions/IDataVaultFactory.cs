using Fdw.Abstractions;
using Fdw.Configuration;

namespace Fdw.Services.DataVault.Abstractions;

/// <summary>
/// Marker interface for data vault factories.
/// </summary>
public interface IDataVaultFactory
{
}

/// <summary>
/// Generic interface for data vault factories with typed configuration.
/// </summary>
/// <typeparam name="TVault">The data vault service type to create.</typeparam>
/// <typeparam name="TConfiguration">The configuration type for the vault.</typeparam>
/// <remarks>
/// Builds from configuration alone — no pre-resolved dependencies are pushed in. A vault's
/// configuration only carries pointers (a connection name, a secret-manager name, a pepper secret
/// name); resolving those into a connection and a pepper is genuinely asynchronous I/O, so this
/// composes <see cref="IAsyncServiceFactory{TService}"/> exactly as <c>IConnectionFactory{TConnection,TConfiguration}</c>
/// does — the implementing factory resolves its own dependencies (constructor-injected) inside its
/// <see cref="IAsyncServiceFactory{TService}.Create"/> override.
/// </remarks>
public interface IDataVaultFactory<TVault, TConfiguration> : IDataVaultFactory, IServiceFactory<TVault, TConfiguration>, IAsyncServiceFactory<TVault>
    where TVault : IDataVault
    where TConfiguration : IGenericConfiguration
{
}
