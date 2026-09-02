using Fdw.Services.Configuration;
using Fdw.Services.Credentials.Sql.Commands;
using Fdw.Services.Credentials.Sql.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Credentials.Sql;

/// <summary>Reads which credential service this host resolves SQL credentials through.</summary>
public class CredentialsSqlConfigurationProvider
    : ImplementationConfigurationProviderBase<CredentialsSqlConfiguration, CredentialsSqlConfigurationCommand>
{
    /// <summary>Initializes a new instance of the <see cref="CredentialsSqlConfigurationProvider"/> class.</summary>
    /// <param name="logger">The logger for this provider.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the server tier.</param>
    /// <param name="dataStoreName">The store the row lives in.</param>
    /// <param name="pathName">The path the row lives under.</param>
    public CredentialsSqlConfigurationProvider(
        ILogger<CredentialsSqlConfigurationProvider>? logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "settings")
        : base(logger ?? NullLogger<CredentialsSqlConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName,
               pathName)
    {
    }
}
