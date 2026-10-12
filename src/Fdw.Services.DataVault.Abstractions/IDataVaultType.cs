using Fdw.Configuration;
using Fdw.ServiceTypes;

namespace Fdw.Services.DataVault.Abstractions;

/// <summary>
/// Marker interface for data vault service type definitions.
/// </summary>
public interface IDataVaultType : IServiceType
{
}

/// <summary>
/// Generic interface for data vault service type definitions with typed parameters.
/// </summary>
/// <typeparam name="TService">The data vault service type.</typeparam>
/// <typeparam name="TFactory">The factory type for creating vault instances.</typeparam>
/// <typeparam name="TConfiguration">The configuration type for the vault.</typeparam>
public interface IDataVaultType<TService, TFactory, TConfiguration>
    : IDataVaultType, IServiceType<System.Guid, TService, TFactory, TConfiguration>
    where TService : IDataVault
    where TConfiguration : IGenericConfiguration
    where TFactory : IDataVaultFactory<TService, TConfiguration>
{
}
