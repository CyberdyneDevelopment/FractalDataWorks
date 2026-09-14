using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Settings;
using Fdw.UI.Themes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.Themes.Registration;

/// <summary>
/// The UI theme member of the settings domain: registers <see cref="ThemeConfigurationProvider"/>
/// and its implementation-row provider.
/// </summary>
/// <remarks>
/// Extracted from <c>ReferenceConfiguration.Endpoints.ThemeEndpointOptions.ThemeEndpoints</c> (FDW-727),
/// where this registration was entangled with being a theme-CRUD endpoint group -- any host that only
/// needed <c>ScalarDocumentation</c>'s theme lookup (every host serving API docs) had to reference the
/// whole endpoints package to get it. A host now gets just the registration by referencing this package.
/// </remarks>
[ExcludeFromCodeCoverage]
[Implementation(typeof(SettingsServiceTypes), "Theme")]
public sealed class ThemeServiceType : SettingsServiceTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ThemeServiceType"/> class.
    /// </summary>
    public ThemeServiceType()
        : base(
            "Theme",
            "Settings:Theme",
            "UI Theme",
            "UI theme configuration read by ScalarDocumentation and the theme endpoints")
    {
        Registration((builder, loggerFactory) =>
        {
            builder.Services.AddSingleton<IThemeImplementationConfigurationProvider, ThemeImplementationConfigurationProvider>(sp => new ThemeImplementationConfigurationProvider(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<ThemeImplementationConfigurationProvider>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                SettingsServiceTypes.ConfigurationConnection));

            builder.Services.TryAddSingleton<ThemeConfigurationProvider>(sp =>
            {
                var domain = new ThemeConfigurationProvider(
                    sp.GetRequiredService<ILogger<ThemeConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    SettingsServiceTypes.ConfigurationConnection);
                domain.Register("Theme", sp.GetRequiredService<IThemeImplementationConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<IThemeConfigurationProvider>(sp => sp.GetRequiredService<ThemeConfigurationProvider>());

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }
}
