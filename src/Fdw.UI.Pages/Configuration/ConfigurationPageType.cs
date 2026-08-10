using Fdw.Collections.Attributes;
using Fdw.UI.Navigation;

namespace Fdw.Configuration.UI.Pages;

/// <summary>
/// Contributes this package's Configuration pages to <see cref="PageTypes"/>.
/// </summary>
[TypeOption(typeof(PageTypes), "Configuration")]
public sealed class ConfigurationPageType : PageTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationPageType"/> class.
    /// </summary>
    public ConfigurationPageType()
        : base(3, "Configuration",
        [
            new Page("Configuration", typeof(global::Fdw.UI.Pages.Configuration.Pages.ConfigurationPage), new NavItem("Settings", "settings", NavSections.Configuration, 90), PageAccess.RequiringPermission("configurations:read")),
        ])
    { }
}
