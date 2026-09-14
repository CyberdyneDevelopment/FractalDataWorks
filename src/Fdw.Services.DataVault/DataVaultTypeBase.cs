using System;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.DataVault.Abstractions;
using Microsoft.Extensions.Logging;

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
    ServiceTypeBase<TService, TFactory, TConfiguration>,
    IDataVaultType<TService, TFactory, TConfiguration>
    where TService : IDataVault
    where TFactory : IDataVaultFactory<TService, TConfiguration>
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
               defaultDataStoreName: "PlatformConfiguration",
               defaultPathName: "sec",
               defaultContainerName: "DataVault")
    {
    }

    // ── Domain-provider/domain-configuration-provider registration ─────────────────────────────
    // An overload of Registration/Register, not a new phase and not a new method name. Stored and
    // invoked exactly like the DI-wiring Registration(Func<IHostApplicationBuilder, ...>) overload
    // already inherited from ServiceTypeBase -- this is simply a second signature of the same verb,
    // distinguished by its parameter types.

    private Func<IServiceProvider, IDataVaultProvider?, IDataVaultConfigurationProvider?, ILogger<IDataVaultType>, IGenericResult> _domainRegistrationMethod
        = static (_, _, _, _) => GenericResult.Success();

    /// <inheritdoc/>
    public void Registration(Func<IServiceProvider, IDataVaultProvider?, IDataVaultConfigurationProvider?, ILogger<IDataVaultType>, IGenericResult> method)
    {
        _domainRegistrationMethod = method;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Base default: an option that never called the overload above reports success and leaves
    /// both providers untouched.
    /// </remarks>
    public IGenericResult Register(IServiceProvider serviceProvider, IDataVaultProvider? domainProvider, IDataVaultConfigurationProvider? domainConfigurationProvider, ILogger<IDataVaultType> logger)
        => _domainRegistrationMethod(serviceProvider, domainProvider, domainConfigurationProvider, logger);
}
