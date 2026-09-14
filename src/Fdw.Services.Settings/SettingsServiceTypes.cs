using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Services.Settings;

/// <summary>
/// ServiceTypeCollection for settings domain service types.
/// </summary>
/// <remarks>
/// Why open, not restricted: this collection previously held only same-assembly "Default"
/// (Server/Tenant/Role settings). Relaxed for FDW-727 so Fdw.UI.Themes.Registration can attach a
/// "Theme" implementation from its own package -- ScalarDocumentation's ThemeConfigurationProvider
/// dependency otherwise forces every host to reference the much larger ReferenceConfiguration.Endpoints
/// package just to get Theme registered. 26 of the 31 ServiceTypeCollections in FDW already default
/// to open; this brings Settings in line with the general plugin-extensibility convention rather than
/// the small closed-set minority (Users/Quality/Operations/Calculation).
/// </remarks>
[ExcludeFromCodeCoverage]
[ServiceTypeCollection(
    typeof(SettingsServiceTypeBase),
    typeof(ISettingsServiceType),
    typeof(SettingsServiceTypes),
    ServiceCategory = "Settings")]
public partial class SettingsServiceTypes : ServiceTypeCollectionBase<SettingsServiceTypeBase, ISettingsServiceType>
{
    /// <summary>
    /// The connection this domain's configuration rows are read from and written to.
    /// </summary>
    public static string ConfigurationConnection { get; set; } = "PlatformConfiguration";

}
