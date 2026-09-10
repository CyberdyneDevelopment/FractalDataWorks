using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Dataverses.Abstractions;
using Fdw.Services.Dataverses.Commands;
using Fdw.Services.Dataverses.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Dataverses;

/// <summary>Supplies the Dataverse configuration.</summary>
public sealed class DataverseConfigurationProvider
    : DomainConfigurationProviderBase<IDataverseImplementationConfiguration>,
      IDataverseConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="DataverseConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    public DataverseConfigurationProvider(
        ILogger<DataverseConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName)
        : base(logger, gatewayProvider, dataStoreName, "dataverse", "Dataverse")
    {
    }
}
