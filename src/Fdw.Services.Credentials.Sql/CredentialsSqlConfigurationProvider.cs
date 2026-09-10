using Fdw.Services.Configuration;
using Fdw.Services.Credentials.Sql.Commands;
using Fdw.Services.Credentials.Sql.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Credentials.Sql;

/// <summary>Reads which credential service this host resolves SQL credentials through.</summary>
public class CredentialsSqlConfigurationProvider
    : ImplementationConfigurationProviderBase<CredentialsSqlConfiguration, ICredentialsSqlImplementationConfiguration, CredentialsSqlConfigurationCommand>
{
    /// <summary>Gets or sets the domain this implementation belongs to.</summary>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <summary>Initializes a new instance of the <see cref="CredentialsSqlConfigurationProvider"/> class.</summary>
    /// <param name="logger">The logger for this provider.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the server tier.</param>
    /// <param name="dataStoreName">The store the row lives in.</param>
    /// <param name="pathName">The path the row lives under.</param>
    public CredentialsSqlConfigurationProvider(
        ILogger<CredentialsSqlConfigurationProvider>? logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "cred")
        : base(logger ?? NullLogger<CredentialsSqlConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName,
               pathName)
    {
    }
}
