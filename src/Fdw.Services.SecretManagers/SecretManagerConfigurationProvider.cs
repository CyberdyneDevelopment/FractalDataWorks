using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Commands;
using Fdw.Services.SecretManagers.Logging;
using Fdw.ServiceTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Services.SecretManagers;

/// <summary>
/// Domain-specific configuration provider for secret managers.
/// The implementation read (dispatch on
/// <see cref="Fdw.Configuration.IDomainConfiguration.Implementation"/>, e.g.
/// "EnvironmentVariable"/"AzureKeyVault", to load the typed body row and attach it to
/// <see cref="SecretManagerConfiguration.Configuration"/>) is composed uniformly by
/// <see cref="ImplementationConfigurationProviderBase{TDomainConfiguration,TImplementationConfiguration,TCommand}"/>. This subclass additionally captures the
/// concrete typed CLR type (for endpoint deserialization) and a reflection-free factory (for default-body
/// creation on Save), and registers typed providers via the inherited <c>Register</c>.
/// </summary>
public class SecretManagerConfigurationProvider
    : ImplementationConfigurationProviderBase<ISecretManagerImplementationConfiguration>,
      ISecretManagerConfigurationProvider
{




    private readonly ILogger<SecretManagerConfigurationProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretManagerConfigurationProvider"/> class.
    /// </summary>
    public SecretManagerConfigurationProvider(
        ILogger<SecretManagerConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "sec")
        : base(logger ?? NullLogger<SecretManagerConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName, pathName,
               "SecretManager")
    {
        _logger = logger ?? NullLogger<SecretManagerConfigurationProvider>.Instance;
    }
}
