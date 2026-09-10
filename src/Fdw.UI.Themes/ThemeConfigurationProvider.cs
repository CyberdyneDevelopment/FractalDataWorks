using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.UI.Themes.Commands;
using Fdw.UI.Themes.Configuration;
using Fdw.UI.Themes.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.Themes;

/// <summary>Supplies the Theme configuration.</summary>
public sealed class ThemeConfigurationProvider
    : DomainConfigurationProviderBase<IThemeImplementationConfiguration>,
      IThemeConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="ThemeConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    public ThemeConfigurationProvider(
        ILogger<ThemeConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName)
        : base(logger, gatewayProvider, dataStoreName, "settings", "Theme")
    {
    }
}
