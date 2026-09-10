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
    public SavedViewImplementationConfigurationProvider(
        ILogger<SavedViewImplementationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "dataverse", "SavedViewImplementation")
    {
    }
}
