using System;
using System.Collections.Generic;
using Fdw.Configuration;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Credentials.Abstractions;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Services.Credentials;

/// <summary>
/// Domain-specific configuration provider for credential services.
/// The polymorphic typed-body read (dispatch on <c>ServiceOptionType</c> to load the typed body row and
/// attach it to <see cref="CredentialServiceConfiguration.Configuration"/>) is composed uniformly by
/// <see cref="ImplementationConfigurationProviderBase{TConfig,TCommand}"/>; typed providers are registered via the
/// inherited <c>Register</c>.
/// </summary>
public class CredentialServiceConfigurationProvider
    : ServiceConfigurationProviderBase<
          CredentialServiceConfiguration,
          ICredentialServiceImplementationConfiguration,
          CredentialServiceConfigurationCommand>,
      ICredentialServiceConfigurationProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CredentialServiceConfigurationProvider"/> class.
    /// </summary>
    public CredentialServiceConfigurationProvider(
        ILogger<CredentialServiceConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName = "ConfigurationDb",
        string pathName = "sec")
        : base(logger ?? NullLogger<CredentialServiceConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName, pathName)
    {
    }

    /// <inheritdoc />
    protected override CredentialServiceConfiguration Compose<T>(
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
