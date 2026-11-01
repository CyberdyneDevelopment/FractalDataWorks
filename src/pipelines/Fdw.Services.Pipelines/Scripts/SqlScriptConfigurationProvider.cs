using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Scripts;

/// <summary>Supplies the implementation row of a stored SQL script and its parameters.</summary>
public sealed class SqlScriptConfigurationProvider
    : ImplementationProviderBase<SqlScriptConfiguration, ISqlScriptImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="SqlScriptConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    /// <param name="pathName">The schema its rows live in.</param>
    /// <param name="containerName">The container its rows live in.</param>
    public SqlScriptConfigurationProvider(
        ILogger<SqlScriptConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
    }
}
