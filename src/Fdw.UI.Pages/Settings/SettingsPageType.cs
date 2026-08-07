using Fdw.Collections.Attributes;
using Fdw.UI.Registration;

namespace Fdw.Services.Settings.UI.Pages;

/// <summary>
/// Contributes this package's Settings pages to <see cref="PageTypes"/>.
/// </summary>
[TypeOption(typeof(PageTypes), "Settings")]
public sealed class SettingsPageType : PageTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsPageType"/> class.
    /// </summary>
    public SettingsPageType()
        : base(6, "Settings",
        [
            new Page("Settings", typeof(global::Fdw.UI.Pages.Settings.Pages.SettingsPage), new NavItem("Settings", "gear", NavSections.Administration, 50), PageAccess.Authenticated),
        ])
    { }
}
