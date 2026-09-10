using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Dataverses;

/// <summary>Supplies the Note implementation's own configuration.</summary>
public sealed class NoteImplementationConfigurationProvider
    : ImplementationProviderBase<NoteImplementationConfiguration, INoteImplementationConfiguration>,
      INoteImplementationConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="NoteImplementationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public NoteImplementationConfigurationProvider(
        ILogger<NoteImplementationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "dataverse", "NoteImplementation")
    {
    }
}
