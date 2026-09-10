using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Dataverses.Commands;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Dataverses;

/// <summary>Supplies the Note configuration.</summary>
public sealed class NoteConfigurationProvider
    : DomainConfigurationProviderBase<INoteImplementationConfiguration>,
      INoteConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="NoteConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public NoteConfigurationProvider(
        ILogger<NoteConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "dataverse", "Note")
    {
    }
}
