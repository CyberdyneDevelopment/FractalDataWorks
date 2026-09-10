using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Commands;
using Fdw.Services.Data.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data;

/// <summary>Reads the data gateway domain's records and routes to the implementation that owns each.</summary>
public class DataGatewayDomainConfigurationProvider
    : ImplementationConfigurationProviderBase<DataGatewayDomainConfiguration, IDataGatewayDomainImplementationConfiguration, DataGatewayDomainConfigurationCommand>,
      IDataGatewayConfigurationProvider
{
    /// <summary>Gets or sets the domain this implementation belongs to.</summary>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <summary>Initializes a new instance of the <see cref="DataGatewayDomainConfigurationProvider"/> class.</summary>
    /// <param name="logger">The logger for this provider.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration connection.</param>
    /// <param name="dataStoreName">The store the records live in.</param>
    /// <param name="pathName">The path the records live under.</param>
    public DataGatewayDomainConfigurationProvider(
        ILogger<DataGatewayDomainConfigurationProvider>? logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "dg")
        : base(logger ?? NullLogger<DataGatewayDomainConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName,
               pathName)
    {
    }

    /// <inheritdoc/>
}
