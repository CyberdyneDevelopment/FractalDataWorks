using Fdw.Collections.Attributes;
using Fdw.UI.Navigation;

namespace Fdw.Services.Connections.UI.Pages;

/// <summary>
/// Contributes this package's Connections pages to <see cref="PageTypes"/>.
/// </summary>
[TypeOption(typeof(PageTypes), "Connections")]
public sealed class ConnectionsPageType : PageTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionsPageType"/> class.
    /// </summary>
    public ConnectionsPageType()
        : base(10, "Connections",
        [
            new Page("ConnectionEditor", typeof(global::Fdw.UI.Pages.Connections.Pages.ConnectionEditorPage), NavItem.Empty, PageAccess.Authenticated),
            new Page("ConnectionWizard", typeof(global::Fdw.UI.Pages.Connections.Pages.ConnectionWizardPage), NavItem.Empty, PageAccess.Authenticated),
            new Page("Connections", typeof(global::Fdw.UI.Pages.Connections.Pages.ConnectionsPage), new NavItem("Connections", "link", NavSections.DataSources, 10), PageAccess.RequiringPermission("connections:read")),
        ])
    { }
}
