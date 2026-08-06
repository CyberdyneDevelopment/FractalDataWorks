using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;

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
public interface IDataVaultFactory<TVault, TConfiguration> : IDataVaultFactory, IServiceFactory<TVault, TConfiguration>
    where TVault : IDataVault
    where TConfiguration : IGenericConfiguration
{
    /// <summary>
    /// Creates a fully-resolved vault. The vault is a pure construction over the supplied
    /// resolved <paramref name="connection"/> and <paramref name="pepper"/> — there is no async
    /// initialization. The provider resolves the connection and pepper ONCE (system context) before
    /// calling this; the factory never resolves anything itself.
    /// </summary>
    /// <param name="configuration">The composed vault configuration header (carries the vault name and typed body).</param>
    /// <param name="connection">The resolved data connection the vault rides.</param>
    /// <param name="pepper">The resolved pepper (HMAC key) bytes; ownership transfers to the vault.</param>
    IGenericResult<TVault> Create(TConfiguration configuration, IDataConnection connection, byte[] pepper);
}
