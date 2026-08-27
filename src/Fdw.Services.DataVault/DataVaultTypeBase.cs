using Fdw.Configuration;
using Fdw.ServiceTypes;
using Fdw.Services.DataVault.Abstractions;

namespace Fdw.Services.DataVault;

/// <summary>
/// Base class for data vault service type definitions that inherit from ServiceTypeBase.
/// Provides vault-specific metadata (category, storage location).
/// </summary>
/// <typeparam name="TService">The data vault service type.</typeparam>
/// <typeparam name="TFactory">The factory type for creating vault instances.</typeparam>
/// <typeparam name="TConfiguration">The vault configuration type.</typeparam>
/// <remarks>
/// Data vault types inherit from this class and supply type metadata in their constructors.
/// Instantiation logic belongs in factories — this class carries metadata only.
/// </remarks>
public abstract class DataVaultTypeBase<TService, TFactory, TConfiguration> :
    // Why: DataVaultProvider (concrete) is the TProvider parameter, not IDataVaultProvider.
    // This matches the ConnectionTypeBase pattern where ConnectionProvider is used so that
    // RegisterFactory implementations can call provider.Register(...) without casting.
    ServiceTypeBase<TService, TFactory, TConfiguration>,
    IDataVaultType<TService, TFactory, TConfiguration>
    where TService : IDataVault
    where TFactory : IDataVaultFactory<TService, TConfiguration>
    // Why: TConfiguration is constrained to IGenericConfiguration (not IDataVaultImplementationConfiguration)
    // because DataVaultServiceTypes registers the root provider with DataVaultConfiguration (the header)
    // as the type parameter. The header implements IGenericConfiguration but not IDataVaultImplementationConfiguration.
    // Factory-level runtime checks enforce typed body correctness.
    where TConfiguration : class, IGenericConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataVaultTypeBase{TService, TFactory, TConfiguration}"/> class.
    /// </summary>
    /// <param name="name">The type option name (e.g. "Default").</param>
    /// <param name="sectionName">The configuration section name for IOptions binding.</param>
    /// <param name="displayName">The human-readable display name.</param>
    /// <param name="description">Description of what this vault type provides.</param>
    /// <param name="category">The service category (defaults to "DataVault").</param>
    protected DataVaultTypeBase(
        string name,
        string sectionName,
        string displayName,
        string description,
        string category = "DataVault")
        : base(name, sectionName, displayName, description, category,
               defaultDataStoreName: "ConfigurationDb",
               defaultPathName: "sec",
               defaultContainerName: "DataVault")
    {
    }
}
