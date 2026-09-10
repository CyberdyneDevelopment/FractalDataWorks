using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Dataverses;

/// <summary>Supplies the Dataverse implementation's own configuration.</summary>
public sealed class DataverseImplementationConfigurationProvider
    : ImplementationProviderBase<DataverseImplementationConfiguration, IDataverseImplementationConfiguration>,
      IDataverseImplementationConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="DataverseImplementationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public DataverseImplementationConfigurationProvider(
        ILogger<DataverseImplementationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "dataverse", "DataverseImplementation")
    {
    }
}
