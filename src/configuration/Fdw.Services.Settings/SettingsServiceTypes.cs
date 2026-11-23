using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Services.Data.Abstractions;

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
    public static string ConfigurationConnection => PlatformConnection.Name;

    /// <summary>Initializes the <see cref="SettingsServiceTypes"/> class: states what a host that registers it must also have registered.</summary>
    static SettingsServiceTypes()
    {
        // The server setting provider writes each setting's description with the descriptions writer, in the unit of the write (FDW-6208): a host that registered this collection
        // without the Descriptions service type is refused here with a coded message, never given a provider that fails on its first save.
        Requires<Fdw.Services.Descriptions.IDescriptionWriter>(Logging.SettingsDescriptionsRegistrationLog.DescriptionWriterNotRegistered);
    }
}
