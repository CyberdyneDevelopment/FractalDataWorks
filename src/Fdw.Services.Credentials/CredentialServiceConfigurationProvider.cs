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
/// The polymorphic typed-body read (dispatch on <c>Implementation</c> to load the typed body row and
/// attach it to <see cref="CredentialServiceConfiguration.Configuration"/>) is composed uniformly by
/// <see cref="ImplementationConfigurationProviderBase{TDomainConfiguration,TImplementationConfiguration,TCommand}"/>; typed providers are registered via the
/// inherited <c>Register</c>.
/// </summary>
public class CredentialServiceConfigurationProvider
    : ImplementationConfigurationProviderBase<ICredentialServiceImplementationConfiguration>,
      ICredentialServiceConfigurationProvider
{

    /// <summary>
    /// Initializes a new instance of the <see cref="CredentialServiceConfigurationProvider"/> class.
    /// </summary>
    public CredentialServiceConfigurationProvider(
        ILogger<CredentialServiceConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "sec")
        : base(logger ?? NullLogger<CredentialServiceConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName, pathName,
               "CredentialService")
    {
    }
}
