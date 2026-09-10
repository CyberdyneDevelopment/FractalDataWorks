using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Dataverses.Commands;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Dataverses;

/// <summary>Supplies the SavedView configuration.</summary>
public sealed class SavedViewConfigurationProvider
    : DomainConfigurationProviderBase<ISavedViewImplementationConfiguration>,
      ISavedViewConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="SavedViewConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    public SavedViewConfigurationProvider(
        ILogger<SavedViewConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName)
        : base(logger, gatewayProvider, dataStoreName, "dataverse", "SavedView")
    {
    }
}
