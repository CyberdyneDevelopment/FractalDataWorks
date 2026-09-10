using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Dataverses.Commands;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Dataverses;

/// <summary>Supplies the SavedView configuration.</summary>
public sealed class SavedViewConfigurationProvider
    : ImplementationConfigurationProviderBase<ISavedViewImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="SavedViewConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public SavedViewConfigurationProvider(
        ILogger<SavedViewConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "dataverse", "SavedView")
    {
    }
}
