using Fdw.Services.Configuration;
using Fdw.Services.Credentials.Sql.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Credentials.Sql;

/// <summary>Supplies the Sql implementation of the credential-store domain.</summary>
/// <remarks>
/// An implementation provider: <c>cred.CredentialsSql</c> has no Name or Implementation of its own and is
/// reached by joining <c>CredentialStoreRowId</c> to its domain row. It is registered into
/// <see cref="CredentialStoreConfigurationProvider"/> and read through it. (FDW-731)
/// </remarks>
public sealed class CredentialsSqlConfigurationProvider
    : ImplementationProviderBase<CredentialsSqlConfiguration, ICredentialStoreImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="CredentialsSqlConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this implementation's configuration rows are read from and written to.</param>
    public CredentialsSqlConfigurationProvider(
        ILogger<CredentialsSqlConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName)
        : base(logger, gatewayProvider, dataStoreName, "cred", "CredentialsSql")
    {
    }
}
