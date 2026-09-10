using Fdw.Services.Quality.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Quality;

/// <summary>Supplies the GlossaryTerm configuration.</summary>
public sealed class GlossaryTermConfigurationProvider
    : DomainConfigurationProviderBase<IGlossaryTermImplementationConfiguration>,
      IGlossaryTermConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="GlossaryTermConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public GlossaryTermConfigurationProvider(
        ILogger<GlossaryTermConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "catalog", "GlossaryTerm")
    {
    }
}
