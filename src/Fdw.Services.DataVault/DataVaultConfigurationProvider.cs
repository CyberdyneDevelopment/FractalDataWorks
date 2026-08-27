using System;
using System.Collections.Generic;
using Fdw.Configuration;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.DataVault.Abstractions;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Services.DataVault;

/// <summary>
/// Domain-specific configuration provider for data vaults.
/// The polymorphic typed-body read (dispatch on <c>ServiceOptionType</c> to load the typed body row and
/// attach it to <see cref="DataVaultConfiguration.Configuration"/>) is composed uniformly by
/// <see cref="ImplementationConfigurationProviderBase{TConfig,TCommand}"/>; typed providers are registered via the
/// inherited <c>Register</c>.
/// </summary>
public class DataVaultConfigurationProvider
    : ServiceConfigurationProviderBase<
          DataVaultConfiguration,
          IDataVaultImplementationConfiguration,
          DataVaultConfigurationCommand>,
      IDataVaultConfigurationProvider
{

    /// <summary>
    /// Initializes a new instance of the <see cref="DataVaultConfigurationProvider"/> class.
    /// </summary>
    public DataVaultConfigurationProvider(
        ILogger<DataVaultConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "sec")
        : base(logger ?? NullLogger<DataVaultConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName, pathName)
    {
    }

    /// <inheritdoc />
    protected override DataVaultConfiguration Compose<T>(
        string serviceOptionType,
        string name,
        T implementationConfiguration)
        => new()
        {
            Name = name,
            ServiceOptionType = serviceOptionType,
            Configuration = implementationConfiguration,
        };
}
