using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Services.Settings;

/// <summary>
/// ServiceTypeCollection for settings domain service types.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeCollection(
    typeof(SettingsServiceTypeBase),
    typeof(ISettingsServiceType),
    typeof(SettingsServiceTypes),
    ServiceCategory = "Settings",
    RestrictToCurrentCompilation = true)]
public partial class SettingsServiceTypes : ServiceTypeCollectionBase<SettingsServiceTypeBase, ISettingsServiceType>
{
    /// <summary>
    /// The connection this domain's configuration rows are read from and written to.
    /// </summary>
    public static string ConfigurationConnection { get; set; } = "PlatformConfiguration";

}
