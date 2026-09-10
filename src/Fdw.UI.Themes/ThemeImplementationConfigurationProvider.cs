using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.UI.Themes.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.Themes;

/// <summary>Supplies the Theme implementation's own configuration.</summary>
public sealed class ThemeImplementationConfigurationProvider
    : ImplementationProviderBase<ThemeImplementationConfiguration, IThemeImplementationConfiguration>,
      IThemeImplementationConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="ThemeImplementationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public ThemeImplementationConfigurationProvider(
        ILogger<ThemeImplementationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "settings", "ThemeImplementation")
    {
    }
}
