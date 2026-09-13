using Fdw.Services.Configuration;
using Fdw.Services.Credentials.Sql.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Credentials.Sql;

/// <summary>Supplies the CredentialStore configuration.</summary>
public sealed class CredentialStoreConfigurationProvider
    : DomainConfigurationProviderBase<ICredentialStoreImplementationConfiguration>,
      ICredentialStoreConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="CredentialStoreConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    public CredentialStoreConfigurationProvider(
        ILogger<CredentialStoreConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName)
        : base(logger, gatewayProvider, dataStoreName, "cred", "CredentialStore")
    {
    }
}
