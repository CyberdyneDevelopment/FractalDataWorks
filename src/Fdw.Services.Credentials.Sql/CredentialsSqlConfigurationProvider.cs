using Fdw.Services.Configuration;
using Fdw.Services.Credentials.Sql.Commands;
using Fdw.Services.Credentials.Sql.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Credentials.Sql;

/// <summary>Supplies the CredentialsSql domain configuration.</summary>
public sealed class CredentialsSqlConfigurationProvider
    : DomainConfigurationProviderBase<ICredentialsSqlImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="CredentialsSqlConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public CredentialsSqlConfigurationProvider(
        ILogger<CredentialsSqlConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "cred", "CredentialsSql")
    {
    }
}
