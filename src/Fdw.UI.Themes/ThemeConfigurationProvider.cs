using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.UI.Themes.Commands;
using Fdw.UI.Themes.Configuration;
using Fdw.UI.Themes.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.UI.Themes;

/// <summary>
/// Configuration provider for themes. Thin wrapper over
/// <see cref="ImplementationConfigurationProviderBase{TConfig,TCommand}"/> with theme-specific logging.
/// </summary>
public class ThemeConfigurationProvider : ImplementationConfigurationProviderBase<ThemeManagedConfiguration, ThemeConfigurationCommand>
{
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="ThemeConfigurationProvider"/> class.</summary>
    public ThemeConfigurationProvider(
        ILogger<ThemeConfigurationProvider>? logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName = "ConfigurationDb",
        string pathName = "settings")
        : base(logger ?? NullLogger<ThemeConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName, pathName)
    {
        _logger = logger ?? NullLogger<ThemeConfigurationProvider>.Instance;
    }

    /// <inheritdoc />
    public override async Task<IGenericResult<IReadOnlyList<ThemeManagedConfiguration>>> Get(
        CancellationToken ct = default)
    {
        var result = await base.Get(ct).ConfigureAwait(false);
        if (result.IsSuccess)
        {
            ThemeConfigurationProviderLog.AllThemesLoaded(_logger, result.Value?.Count ?? 0);
        }
        return result;
    }
}
