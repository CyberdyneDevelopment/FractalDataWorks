using Fdw.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Commands;
using Fdw.Services.Data.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data;

/// <summary>Supplies the DataGateway domain configuration.</summary>
public sealed class DataGatewayDomainConfigurationProvider
    : DomainConfigurationProviderBase<IDataGatewayDomainImplementationConfiguration>,
      IDataGatewayConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="DataGatewayDomainConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public DataGatewayDomainConfigurationProvider(
        ILogger<DataGatewayDomainConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "dg", "DataGateway")
    {
    }
}
