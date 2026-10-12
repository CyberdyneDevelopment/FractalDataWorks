using Fdw.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Hosts.Abstractions;
using Fdw.Services.Hosts.Commands;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Hosts;

/// <summary>
/// Reads the SQL Server endpoint domain and dispatches to the implementation each row names.
/// </summary>
/// <remarks>
/// FDW supplies the provider, not its registration: the host that listens names the implementation it
/// dispatches to, as it does for every host feature.
/// </remarks>
public sealed class SqlServerEndpointConfigurationProvider
    : DomainConfigurationProviderBase<ISqlServerEndpointImplementationConfiguration>,
      ISqlServerEndpointConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="SqlServerEndpointConfigurationProvider"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="gatewayProvider">Provides the configuration gateway rows are read through.</param>
    /// <param name="dataStoreName">The store the domain is read from.</param>
    /// <param name="pathName">The path its containers sit in.</param>
    /// <param name="containerName">The domain's container.</param>
    public SqlServerEndpointConfigurationProvider(
        ILogger<SqlServerEndpointConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
    }
}
