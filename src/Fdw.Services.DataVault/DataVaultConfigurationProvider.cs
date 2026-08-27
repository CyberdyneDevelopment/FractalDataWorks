using System;
using System.Collections.Generic;
using Fdw.Configuration;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
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
public class DataVaultConfigurationProvider : ImplementationConfigurationProviderBase<DataVaultConfiguration, DataVaultConfigurationCommand>
{

    /// <summary>
    /// Initializes a new instance of the <see cref="DataVaultConfigurationProvider"/> class.
    /// </summary>
    public DataVaultConfigurationProvider(
        ILogger<DataVaultConfigurationProvider> logger,
        Lazy<IConfigurationGateway> lazyGateway,
        string dataStoreName = "ConfigurationDb",
        string pathName = "sec")
        : base(logger ?? NullLogger<DataVaultConfigurationProvider>.Instance,
               lazyGateway,
               dataStoreName, pathName)
    {
    }
}
