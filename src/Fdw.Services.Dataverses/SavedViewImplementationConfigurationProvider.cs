using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Dataverses;

/// <summary>Supplies the SavedView implementation's own configuration.</summary>
public sealed class SavedViewImplementationConfigurationProvider
    : ImplementationProviderBase<SavedViewImplementationConfiguration, ISavedViewImplementationConfiguration>,
      ISavedViewImplementationConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="SavedViewImplementationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    public SavedViewImplementationConfigurationProvider(
        ILogger<SavedViewImplementationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName)
        : base(logger, gatewayProvider, dataStoreName, "dataverse", "SavedViewImplementation")
    {
    }
}
